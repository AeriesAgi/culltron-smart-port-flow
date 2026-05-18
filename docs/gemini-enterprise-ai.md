# Gemini enterprise AI layer

Gemini is server-side only, action-triggered only and always protected by deterministic fallback.

## Model routing

- Premium/routine operations model: `Gemini__PremiumModel=gemini-2.5-flash`.
- Lite classifier/extractor: `Gemini__RoutineModel=gemini-2.5-flash-lite` when configured/available.
- Optional deep reasoning: configure `Gemini__DeepReasoningModel` for future large scenario reviews.
- Optional maps/embeddings flags are documented as future switches; the tracker renders from app check-in data without Gemini.

## Actions using Gemini when available

Operations briefs, execution plans, fleet summaries, driver instructions, incident impact summaries, emissions explanations and driver/fleet/control-room Copilot prompts.

## Fallback

If no key, disabled config, quota limit, unsupported model or network failure occurs, deterministic fallback produces transparent operational output and the UI shows source/model/fallback status.

## Quota-control guarantees

- No Gemini call on `/dashboard`, `/fleet`, `/driver-app`, or route navigation.
- Gemini runs only after explicit user action such as **Generate operations brief**, **Ask Copilot**, **Explain execution plan**, **Analyze check-in**, optional **Generate TTS audio**, or optional **Semantic search**.
- The demo consolidates operational context into one prompt per action; it does not loop over trucks and call Gemini per truck.
- UI copy states: “Gemini unavailable or quota-limited. Deterministic Smart Port fallback generated this response.” when the API is disabled, fails, or reaches quota.
- Optional TTS, embeddings, and maps grounding are feature-flagged (`Gemini__UseTts`, `Gemini__UseEmbeddings`, `Gemini__UseMapsGrounding`) and are not required for the core demo.
