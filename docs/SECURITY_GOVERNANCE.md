# Security and Agent Governance

- ASP.NET Identity roles protect operational areas.
- Demo cookies are secondary to Identity and are cleared on sign-out.
- Agent actions require human approval before external sends.
- Secrets are not included in prompts or UI output.
- Demo data is synthetic; no customer data is required.
- Prompt-injection style requests are treated as untrusted instructions.
- WhatsApp live sends require configured credentials and approved test recipients.
- `/agent-governance` demonstrates blocked secrets, approval bypass blocking, bulk-send gating and audit history.
