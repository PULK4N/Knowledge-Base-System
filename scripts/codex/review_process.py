"""Own reviewer processes and cancel them without polling for completion."""

from __future__ import annotations

import ctypes
import os
import signal
import subprocess
import threading
from pathlib import Path
from typing import Callable


class ReviewCancelled(Exception):
    pass


class Cancellation:
    def __init__(self) -> None:
        self.condition = threading.Condition()
        self.cancelled = False
        self.terminate: Callable[[], None] = lambda: None
        self.previous_handlers: dict[int, object] = {}
        self.console_handler = None

    def check(self) -> None:
        if self.cancelled:
            raise ReviewCancelled("Review cancelled")

    def cancel(self) -> None:
        with self.condition:
            self.cancelled = True
            self.condition.notify_all()
        self.terminate()

    def install(self) -> None:
        if os.name == "nt":
            handler_type = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_ulong)

            def handle_console(event: int) -> bool:
                if event in (0, 1, 2, 5, 6):
                    self.cancel()
                    return True
                return False

            self.console_handler = handler_type(handle_console)
            kernel = ctypes.WinDLL("kernel32", use_last_error=True)
            kernel.SetConsoleCtrlHandler.argtypes = [handler_type, ctypes.c_bool]
            kernel.SetConsoleCtrlHandler.restype = ctypes.c_bool
            if not kernel.SetConsoleCtrlHandler(self.console_handler, True):
                raise ctypes.WinError(ctypes.get_last_error())
        else:
            def handle_signal(signum: int, frame: object) -> None:
                self.cancel()
                raise ReviewCancelled("Review cancelled")

            for signum in (signal.SIGINT, signal.SIGTERM):
                self.previous_handlers[signum] = signal.signal(signum, handle_signal)

    def close(self) -> None:
        if self.console_handler is not None:
            kernel = ctypes.WinDLL("kernel32", use_last_error=True)
            kernel.SetConsoleCtrlHandler(self.console_handler, False)
            self.console_handler = None
        for signum, handler in self.previous_handlers.items():
            signal.signal(signum, handler)


if os.name == "nt":
    from ctypes import wintypes
    import msvcrt

    class BasicLimits(ctypes.Structure):
        _fields_ = [
            ("process_time", ctypes.c_int64), ("job_time", ctypes.c_int64),
            ("flags", wintypes.DWORD), ("min_working_set", ctypes.c_size_t),
            ("max_working_set", ctypes.c_size_t), ("active_processes", wintypes.DWORD),
            ("affinity", ctypes.c_size_t), ("priority", wintypes.DWORD),
            ("scheduling", wintypes.DWORD),
        ]

    class ExtendedLimits(ctypes.Structure):
        _fields_ = [
            ("basic", BasicLimits), ("io", ctypes.c_uint64 * 6),
            ("process_memory", ctypes.c_size_t), ("job_memory", ctypes.c_size_t),
            ("peak_process_memory", ctypes.c_size_t), ("peak_job_memory", ctypes.c_size_t),
        ]

    class StartupInfo(ctypes.Structure):
        _fields_ = [
            ("cb", wintypes.DWORD), ("reserved", wintypes.LPWSTR),
            ("desktop", wintypes.LPWSTR), ("title", wintypes.LPWSTR),
            ("x", wintypes.DWORD), ("y", wintypes.DWORD),
            ("x_size", wintypes.DWORD), ("y_size", wintypes.DWORD),
            ("x_chars", wintypes.DWORD), ("y_chars", wintypes.DWORD),
            ("fill", wintypes.DWORD), ("flags", wintypes.DWORD),
            ("show", wintypes.WORD), ("reserved_size", wintypes.WORD),
            ("reserved_bytes", ctypes.POINTER(ctypes.c_byte)),
            ("stdin", wintypes.HANDLE), ("stdout", wintypes.HANDLE),
            ("stderr", wintypes.HANDLE),
        ]

    class ProcessInfo(ctypes.Structure):
        _fields_ = [
            ("process", wintypes.HANDLE), ("thread", wintypes.HANDLE),
            ("pid", wintypes.DWORD), ("thread_id", wintypes.DWORD),
        ]

    class JobAccounting(ctypes.Structure):
        _fields_ = [
            ("user_time", ctypes.c_int64), ("kernel_time", ctypes.c_int64),
            ("period_user_time", ctypes.c_int64), ("period_kernel_time", ctypes.c_int64),
            ("page_faults", wintypes.DWORD), ("total", wintypes.DWORD),
            ("active", wintypes.DWORD), ("terminated", wintypes.DWORD),
        ]


class OwnedProcesses:
    """A Windows job (or POSIX process group) owned solely by this runner."""

    def __init__(self, cancellation: Cancellation) -> None:
        self.cancellation = cancellation
        self.process: subprocess.Popen | None = None
        self.last_pid: int | None = None
        self.job = None
        if os.name == "nt":
            self.kernel = ctypes.WinDLL("kernel32", use_last_error=True)
            signatures = {
                "CreateJobObjectW": ([ctypes.c_void_p, wintypes.LPCWSTR], wintypes.HANDLE),
                "SetInformationJobObject": ([wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD], wintypes.BOOL),
                "AssignProcessToJobObject": ([wintypes.HANDLE, wintypes.HANDLE], wintypes.BOOL),
                "TerminateJobObject": ([wintypes.HANDLE, wintypes.UINT], wintypes.BOOL),
                "CloseHandle": ([wintypes.HANDLE], wintypes.BOOL),
                "ResumeThread": ([wintypes.HANDLE], wintypes.DWORD),
                "TerminateProcess": ([wintypes.HANDLE, wintypes.UINT], wintypes.BOOL),
                "WaitForSingleObject": ([wintypes.HANDLE, wintypes.DWORD], wintypes.DWORD),
                "GetExitCodeProcess": ([wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD)], wintypes.BOOL),
                "QueryInformationJobObject": ([wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD, ctypes.c_void_p], wintypes.BOOL),
                "CreateProcessW": ([wintypes.LPCWSTR, wintypes.LPWSTR, ctypes.c_void_p, ctypes.c_void_p, wintypes.BOOL, wintypes.DWORD, ctypes.c_void_p, wintypes.LPCWSTR, ctypes.POINTER(StartupInfo), ctypes.POINTER(ProcessInfo)], wintypes.BOOL),
            }
            for name, (arguments, result) in signatures.items():
                function = getattr(self.kernel, name)
                function.argtypes = arguments
                function.restype = result
            self.job = self.kernel.CreateJobObjectW(None, None)
            if not self.job:
                raise ctypes.WinError(ctypes.get_last_error())
            limits = ExtendedLimits()
            limits.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            if not self.kernel.SetInformationJobObject(self.job, 9, ctypes.byref(limits), ctypes.sizeof(limits)):
                self.kernel.CloseHandle(self.job)
                self.job = None
                raise ctypes.WinError(ctypes.get_last_error())
        cancellation.terminate = self.terminate

    def terminate(self) -> None:
        if os.name == "nt" and self.job:
            self.kernel.TerminateJobObject(self.job, 130)
        elif self.process is not None:
            try:
                os.killpg(self.process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

    def run(self, command: list[str], cwd: Path, stdout: Path, stderr: Path,
            stdin: Path | None = None, started: Callable[[int], None] | None = None) -> int:
        self.cancellation.check()
        with open(stdin or os.devnull, "rb") as input_stream, stdout.open("wb") as output_stream, stderr.open("wb") as error_stream:
            if os.name == "nt":
                return self._run_windows(command, cwd, input_stream, output_stream, error_stream, started)
            self.process = subprocess.Popen(command, cwd=cwd, stdin=input_stream, stdout=output_stream,
                                            stderr=error_stream, start_new_session=True)
            try:
                self.last_pid = self.process.pid
                if started:
                    started(self.process.pid)
                self.cancellation.check()
                result = self.process.wait()
                self.cancellation.check()
                return result
            finally:
                self.terminate()
                self.process.wait()
                self.process = None

    def _run_windows(self, command: list[str], cwd: Path, input_stream: object,
                     output_stream: object, error_stream: object,
                     started: Callable[[int], None] | None) -> int:
        info = ProcessInfo()
        startup = StartupInfo()
        startup.cb = ctypes.sizeof(startup)
        startup.flags = 0x100  # STARTF_USESTDHANDLES
        handles = [msvcrt.get_osfhandle(stream.fileno()) for stream in (input_stream, output_stream, error_stream)]
        startup.stdin, startup.stdout, startup.stderr = handles
        line = ctypes.create_unicode_buffer(subprocess.list2cmdline(command))
        try:
            for handle in handles:
                os.set_handle_inheritable(handle, True)
            # Suspended launch prevents tools escaping before job assignment.
            if not self.kernel.CreateProcessW(command[0], line, None, None, True,
                                              0x08000004, None, str(cwd), ctypes.byref(startup), ctypes.byref(info)):
                raise ctypes.WinError(ctypes.get_last_error())
        finally:
            for handle in handles:
                os.set_handle_inheritable(handle, False)
        assigned = False
        try:
            if not self.kernel.AssignProcessToJobObject(self.job, info.process):
                raise ctypes.WinError(ctypes.get_last_error())
            assigned = True
            self.last_pid = info.pid
            if started:
                started(info.pid)
            self.cancellation.check()
            if self.kernel.ResumeThread(info.thread) == 0xFFFFFFFF:
                raise ctypes.WinError(ctypes.get_last_error())
            if self.kernel.WaitForSingleObject(info.process, 0xFFFFFFFF) != 0:
                raise ctypes.WinError(ctypes.get_last_error())
            self.cancellation.check()
            exit_code = wintypes.DWORD()
            if not self.kernel.GetExitCodeProcess(info.process, ctypes.byref(exit_code)):
                raise ctypes.WinError(ctypes.get_last_error())
            return exit_code.value
        finally:
            try:
                if assigned:
                    self.terminate()
                    accounting = JobAccounting()
                    if not self.kernel.QueryInformationJobObject(self.job, 1, ctypes.byref(accounting), ctypes.sizeof(accounting), None):
                        raise ctypes.WinError(ctypes.get_last_error())
                    if accounting.active:
                        if self.kernel.WaitForSingleObject(self.job, 10000) != 0:
                            raise RuntimeError("Owned child processes did not terminate")
                else:
                    self.kernel.TerminateProcess(info.process, 130)
            finally:
                self.kernel.CloseHandle(info.thread)
                self.kernel.CloseHandle(info.process)

    def close(self) -> None:
        self.terminate()
        if self.job:
            self.kernel.CloseHandle(self.job)
            self.job = None
        self.cancellation.terminate = lambda: None
