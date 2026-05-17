# Gemini enterprise AI layer

Gemini is server-side only, action-triggered only and always protected by deterministic fallback.

## Model routing

- Premium/routine operations model: `Gemini__PremiumModel=gemini-2.5-flash`.
- Lite classifier/extractor: `Gemini__RoutineModel=gemini-3.1-flash-lite` when configured/available.
- Optional deep reasoning: configure `Gemini__DeepReasoningModel` for future large scenario reviews.
- Optional maps/embeddings flags are documented as future switches; the tracker renders from app check-in data without Gemini.

## Actions using Gemini when available

Operations briefs, execution plans, fleet summaries, driver instructions, incident impact summaries, emissions explanations and driver/fleet/control-room Copilot prompts.

## Fallback

If no key, disabled config, quota limit, unsupported model or network failure occurs, deterministic fallback produces transparent operational output and the UI shows source/model/fallback status.
