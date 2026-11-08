# Code reviewers — planning questions and answers

Recorded planning exchanges below are copied verbatim; later answers supersede earlier suggestions.

### Execution and settings

**Question**

Should all reviewers run in parallel? I recommend preparing the shared cache once per model + reasoning level, then launching the reviewers concurrently so total review time stays shorter.

**Answer**

Reviewers should run in batches. Batches should be configurable through administration - same as the task message for review that we said
`You are a code reviewer thats been provided with feature details, code differences and a skill. Use it to find errors in the code that indicate that skill wasn't followed properly. If skill is not applicable in this scenario, just return with the message that nothing was found.`
Those things should be saved to a simple table in the db, initially migrated. And scritpt should be able to fetch that

**Question**

Should batching mean a configurable **maximum reviewers per batch**, running concurrently, with the next batch starting after the current batch finishes? I recommend this because it controls concurrency with one simple setting.

**Answer**

yes

**Question**

If one reviewer fails, should the script continue with the remaining reviewers and mark that failure in the report? I recommend yes, so completed reviews remain useful.

**Answer**

Yes, probably the all details should be saved somewhere, so that it can be retried. cache guid, etc, maybe we should split into multiple scripts by responsibility. So that maybe some script can be reused for retry or something.

### Direct reruns and saved data

**Question**

Should retries reuse the **original diff, context, and reviewer configuration**? I recommend yes, so a retry remains part of the same review; reviewing updated code would start a new run.

**Answer**

Retry script is probably not needed. Since coordinator runs reviewer runner, and coordinator outputs cache id, which agent failed etc. Then agent running the coordinator can manually just call reviewer runner with parameters to try like that

**Question**

Should the reviewer runner accept **the saved run manifest path and reviewer ID**? I recommend these two parameters so it can recover the original context, model settings, and cache reference without requiring the agent to repeat them manually.

**Answer**

But is reviewer id enough? Since there are both feature reviewers and project reviewers, what is the idea here?

**Question**

Would you prefer these explicit keys? I recommend them because they preserve the distinction between project and feature reviewers without introducing another ID.

**Answer**

Also what is manifest path?

**Question from the user**

Also what is manifest path?

**Answer**

It means **the path to a JSON file describing one review run**, for example:

`review-runs/run-123/run.json`

The coordinator creates it and saves:

- Feature, plan, and compared commit IDs.
- Locations of the captured context and diff.
- Reviewers, their instructions, model settings, and cache IDs.
- Each reviewer’s status and output locations.

The reviewer runner reads that file plus the selected reviewer key to recover everything needed for another attempt.

“Saved run file” is a clearer name for it.

**Further clarification from the user**

I'm not sure feature plan id etc. what was cached is needed

**Answer**

Yes—the cached session already contains the feature overview, plan, review notes, and diff. **The runner doesn’t need their IDs again.**

To rerun a reviewer, it needs:

- The cached session ID.
- Model + reasoning level.
- Reviewer-specific input: skill and optional note, or feature reviewer instructions.
- An output location.

The coordinator can save and print those details for each reviewer. A separate saved run file wouldn’t need to be a required runner parameter.

**Further clarification from the user**

Either way, on the reviewers run, that part as text can be saved into a separate file, just so that we have it in a readable format. Like that can be a thing coordinator does.
And yes, those other things can be saved as well

### Defaults and feature-reviewer task

**Question**

What should the initial batch size be? I suggest **3 reviewers per batch** as a starting default, editable through Administration.

**Answer**

make it 5 per batch as initial

**Question**

Should the shared task message explicitly tell feature reviewers to follow their saved reviewer instructions when no skill is supplied? I recommend yes, so they don’t interpret the absence of a skill as “not applicable” and return nothing found.

**Answer**

Obviously yes

**Additional direction from the user**

Note that settings should also be in kind of settings state machine
