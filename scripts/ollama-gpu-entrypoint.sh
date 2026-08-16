#!/bin/sh

set -u

ollama_pid=""
log_tail_pid=""
shutdown_requested=0
ollama_log="/tmp/ollama-server.log"
gpu_ready_file="/tmp/ollama-gpu-ready"

gpu_is_available() {
    command -v nvidia-smi >/dev/null 2>&1 &&
        nvidia-smi >/dev/null 2>&1
}

request_shutdown() {
    shutdown_requested=1

    if [ -n "${ollama_pid}" ] && kill -0 "${ollama_pid}" 2>/dev/null; then
        kill -TERM "${ollama_pid}" 2>/dev/null || true
    fi

    if [ -n "${log_tail_pid}" ] && kill -0 "${log_tail_pid}" 2>/dev/null; then
        kill -TERM "${log_tail_pid}" 2>/dev/null || true
    fi
}

cleanup() {
    if [ -n "${log_tail_pid}" ] && kill -0 "${log_tail_pid}" 2>/dev/null; then
        kill -TERM "${log_tail_pid}" 2>/dev/null || true
        wait "${log_tail_pid}" 2>/dev/null
    fi

    rm -f "${gpu_ready_file}" "${ollama_log}"
}

kill_unsafe_ollama() {
    echo "$1" >&2
    kill -KILL "${ollama_pid}" 2>/dev/null || true
    wait "${ollama_pid}" 2>/dev/null
    exit 1
}

trap 'request_shutdown' HUP INT TERM
trap 'cleanup' 0

if ! gpu_is_available; then
    echo "NVIDIA GPU is unavailable; refusing to start Ollama." >&2
    exit 1
fi

rm -f "${gpu_ready_file}"
: >"${ollama_log}"

tail -n +1 -F "${ollama_log}" &
log_tail_pid=$!

ollama "$@" >"${ollama_log}" 2>&1 &
ollama_pid=$!

gpu_discovery_waited=0

while kill -0 "${ollama_pid}" 2>/dev/null; do
    if grep -Eiq 'msg="inference compute".*library=[^ ]*cuda' "${ollama_log}"; then
        touch "${gpu_ready_file}"
        break
    fi

    if grep -Eq 'msg="inference compute"' "${ollama_log}"; then
        kill_unsafe_ollama \
            "Ollama did not register a CUDA inference backend; refusing CPU fallback."
    fi

    if [ "${gpu_discovery_waited}" -ge 30 ]; then
        kill_unsafe_ollama \
            "Ollama CUDA discovery timed out; refusing to continue."
    fi

    if [ "${shutdown_requested}" -eq 1 ]; then
        break
    fi

    sleep 1
    gpu_discovery_waited=$((gpu_discovery_waited + 1))
done

while kill -0 "${ollama_pid}" 2>/dev/null; do
    sleep 10

    if [ "${shutdown_requested}" -eq 1 ]; then
        break
    fi

    if ! kill -0 "${ollama_pid}" 2>/dev/null; then
        break
    fi

    if ! gpu_is_available; then
        kill_unsafe_ollama \
            "NVIDIA GPU became unavailable; stopping Ollama immediately."
    fi

    if ollama ps 2>/dev/null | sed '1d' | grep -q 'CPU'; then
        kill_unsafe_ollama \
            "Ollama loaded a model on the CPU; stopping it immediately."
    fi
done

wait "${ollama_pid}"
exit $?
