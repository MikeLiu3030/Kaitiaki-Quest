# AI Agent Instructions - Kaitiaki Quest

> **Purpose**: This document records a second, distinct mode of AI-assisted work on this project: using **Claude Code**, an agentic AI with direct file-system, shell, and sub-agent orchestration access, to build and maintain the project's own `/specs` evidence documentation. [03-AI-prompts.md](03-AI-prompts.md) records *conversational* AI tool usage (DeepSeek/Gemini/Qwen) during feature development; this file records *agentic* AI usage — an AI directing its own multi-step tool use under human instruction and review, rather than a human copying code out of a chat window.

## Why this file exists separately from 03-AI-prompts.md

`03-AI-prompts.md` is evidence that conversational AI tools accelerated the coding work. This file is evidence of a different, additional capability that was used specifically to *produce that evidence itself*: turning 35 raw, mixed-language AI conversation exports (one of them 74,876 lines / ~2.9MB) into a structured, verified, English-language record, cross-checked against `git log`. Doing this by hand would have meant manually reading and translating well over 100,000 lines of chat transcript. Instead, the task was delegated to Claude Code, which planned and executed the extraction itself — this is recorded here as its own piece of AI-assisted-development evidence.

---

## Session Overview

### 1. Recognizing the English-language requirement
**Instruction:** The author had been developing the project while communicating with AI tools in Chinese, but realized the MSA brief requires the `/specs` evidence to be in English, and didn't know how to reconcile the two.
**Agent action:** Reviewed the existing `03-AI-prompts.md` placeholder alongside the already-English `01-planning.md`/`02-architecture.md` for tone and structure, and explained that the fix is translation at the point of recording, not re-doing the original development conversations in English.
**Human decision:** Asked the agent to proceed with a first-draft translation.

### 2. Rejecting the first attempt: a prompt list is not evidence
**Instruction:** The author supplied a manually-compiled list of 93 prompts (in Chinese) from memory and asked the agent to translate and insert them directly into `03-AI-prompts.md`.
**Agent action:** Reviewed the list against `03-AI-prompts.md`'s own stated purpose ("records the prompts used, **the AI responses received**, and **the human decisions made**") and identified that a prompts-only list satisfies none of the "response" or "decision" evidence requirements, and remained non-English regardless. Proposed a Prompt / AI Response Summary / Human Review & Decision structure instead.
**Human decision:** Agreed, and separately raised that development had used **multiple AI tools** (a main planning assistant plus others for point-fixes), and that the original responses from those other tools were not copied into the conversation.
**Agent action:** Explained that reconstructing an accurate "AI Response" field doesn't require the verbatim original text — it can be a faithful summary cross-checked against what actually ended up in the codebase (the ground truth), with tool attribution added per entry.

### 3. Grounding the record in the commit history
**Instruction:** "Go analyze my git log and help me fill this in."
**Agent action:** Ran `git log --reverse --date=short --pretty=format:"%h|%ad|%s"` to get the full, chronological commit history, then mapped commit clusters to the 6 development phases already implied by the manually-compiled prompt list (Ideation → Backend → Frontend → Backend Testing → Controller Testing → Deployment). Wrote a first structured `03-AI-prompts.md` with real commit-hash references, flagging a handful of `[confirm]` gaps where the commit history alone couldn't resolve which tool was used or exactly which commit fixed a recurring bug.

### 4. Supplying the actual raw logs
**Instruction:** The author shared a DeepSeek chat-share URL, then — once fetching it failed (403, blocked for scrapers) — exported and organized **35 markdown files** from three tools (1 DeepSeek conversation covering the whole project, 17 Gemini sessions, 17 Qwen sessions) into a folder, and asked the agent to read and analyze all of it.
**Agent action:** This is where the agentic (not just conversational) nature of the tool mattered. The approach:

1. **Surveyed the folder** with `Glob` and `wc -l` to find file sizes before attempting anything — one file (`DeepSeek 游戏化项目创意推荐.md`) was 74,876 lines / 2.9MB, two Gemini files were 17,397 and 6,972 lines, and the rest ranged from ~40 to ~3,500 lines.
2. **Checked the actual format** of each log type by reading a sample (DeepSeek used `**提问：**`/`**回答：**` markers with occasional internal reasoning blockquotes; Gemini exports used `# you asked` timestamped blocks, often containing large pasted source files for review).
3. **Estimated token budget** (byte size ÷ ~39 bytes/line, accounting for UTF-8 multi-byte Chinese text) and determined the largest files could not be read in a single pass by any one agent, so split them by line-count using `sed -n`, snapping split points to clean question/answer boundaries (found via `grep -n` on the `**提问：**` marker) rather than arbitrary line numbers — 8 chunks for the DeepSeek log, 2 for the largest Gemini file.
4. **Dispatched 17 parallel sub-agents** (Claude's `general-purpose` agent type), one per chunk/file-group, each with a self-contained instruction set (see template below) specifying: the project context, the exact file(s) to read, the log's format quirks, the required output structure (Prompt / AI Response summary / Outcome-Decision), and explicit rules (skip filler, merge tightly-coupled debugging threads into one entry, translate to English, stay concise, return output as text rather than writing files).
5. **Tracked progress via `TodoWrite`** across the multi-hour, multi-batch run, and reported each of the 17 completions to the human as they arrived — without fabricating or guessing at results still in flight, per the constraint that background agent output cannot be predicted before it actually returns.
6. **Synthesized all 17 reports** into a single restructured `03-AI-prompts.md`, organized by development phase (not by source file), with a dedicated case-study section for the SignalR real-time-sync debugging arc once it became clear this thread spanned all three tools and was the richest evidence of iterative AI-assisted debugging in the whole dataset. Two previously-unresolved `[confirm]` gaps from step 3 were resolved directly from the raw logs (a login-redirect bug traced to an `autho-storage`/`auth-storage` typo; the ASP.NET test-runner debugging tool identified as Gemini).
7. **Cleaned up** the temporary split files from the working scratchpad once the merge was verified, leaving no intermediate artifacts in the repository.

**Human decision:** Reviewed the final merged document; raised (separately) that the raw source logs live outside the repo given their size, as a follow-up consideration rather than a blocking issue.

---

## Sub-agent Instruction Template

Each of the 17 parallel extraction agents received a self-contained prompt of this shape (illustrative example, DeepSeek chunk):

```
You are helping build an academic "AI Prompts Record" document (in English) for
[project context]. You have been given ONE chunk (N of 8, in chronological order)
of the full exported chat log.

File to read: [scratchpad path]

Format notes: [log-specific parsing guidance — markers, what to skip, chunk-boundary caveats]

Your task: Extract EVERY distinct, technically or decision-meaningful exchange in
this chunk, in chronological order, translated to English. For each, output:

### <short topic title>
**Prompt (translated):** <1-3 sentences, preserving key specifics>
**AI Response (summary):** <2-4 sentences, summarized not verbatim>
**Outcome/Decision (if shown in the log):** <what was adopted/changed, or "not stated">

Rules:
- Skip pure filler exchanges with zero technical content.
- Merge a tightly-coupled back-and-forth into ONE entry if it's one debugging thread.
- Be concise — this is for merging into a larger document, not a translation exercise.
- Do NOT write to any file — return your full output as your final message text.
```

This template was adapted per batch with the specific file paths, expected content
(e.g. "this chunk likely covers backend testing debugging"), and any format quirks
particular to that tool's export (Gemini's pasted-code blocks, Qwen's uninformative
generic filenames requiring topic inference from content).

---

## Tooling Summary

| Capability used | Purpose |
| :--- | :--- |
| `Glob` / `Bash` (`wc -l`, `grep -n`, `sed -n`) | Surveying and splitting oversized source files at clean boundaries before any AI reads them |
| `Agent` (parallel dispatch, background execution) | Running 17 independent extraction-and-translation passes concurrently instead of sequentially exhausting one context window |
| `TodoWrite` | Tracking a long-running, multi-batch task across many conversation turns without losing state |
| `Write` / `Edit` | Producing the final merged, human-reviewed `specs/03-AI-prompts.md` |

**Human oversight throughout:** every structural decision (phase grouping, the SignalR case-study callout, resolving the `[confirm]` gaps, retaining honest "reverted" outcomes like the React 19 `use()` experiment rather than only recording successes) was reviewed and could be redirected by the author at each step; the agent's outputs were treated as a draft to verify against the actual codebase and commit history, not as final truth.
