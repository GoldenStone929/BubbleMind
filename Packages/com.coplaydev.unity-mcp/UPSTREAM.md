# Project-Isolated MCP for Unity

This embedded package is based on `CoplayDev/unity-mcp` v10.1.0 at commit
`c14de1e6dc01ab42d2bb358730cff954bce0ce6b`.

The project patch keeps the stdio bridge functionality while enforcing these
local safety constraints:

- client configuration rewrites and legacy migrations are opt-in only;
- HTTP auto-start, HTTP reload, and automatic setup UI are opt-in only;
- automatic machine-global test throttling is opt-in only;
- machine-global Unity `EditorPrefs` reads and writes used by the active stdio
  bridge path are disabled unless explicitly opted in;
- port registry and heartbeat files require `UNITY_MCP_STATUS_DIR` inside this
  Unity project;
- stdio reload intent uses Unity `SessionState`, not machine-global
  `EditorPrefs`;
- package-level stdio auto-start is disabled so the project bootstrap owns the
  bridge lifecycle.
- scene file paths are canonicalized and rejected if they escape this
  project's `Assets/` directory.

The original MIT license is preserved in `LICENSE`.
