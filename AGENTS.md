# GenericGachaRPG 持久执行规则

这些规则适用于本 Unity 工程内的所有 Codex 任务。

## 启动顺序

1. 确认当前工程根目录直接包含 `Assets/`、`Packages/`、`ProjectSettings/` 与 `PROJECT_PLAN.md`。
2. 完整阅读 `PROJECT_PLAN.md`，再阅读 `StudioOps/CURRENT_MILESTONE.md` 和与任务有关的文档。
3. 检查 Git 状态和正在运行的 Unity 实例；保留用户已有改动，不擅自清理或覆盖。
4. 以实际文件、Unity Console、测试和构建结果为准，不以旧对话或推测代替验证。

## 沟通与权限

- 所有面向用户的沟通、进度和交付说明使用中文；代码标识符可使用英文。
- 用户允许长期、持续执行已批准范围内的工作；只有下载新依赖、产生费用、需要账号/API Key、发布远程仓库或实质扩大范围时才暂停请求授权。
- 未经明确授权，不创建或推送远程仓库，不发布构建，不联系外部服务。

## 路径与安全边界

- 可写范围仅限本工程根目录及其子目录。
- `..\analysis\` 仅可按需只读；不得修改。
- 不读取、运行、解包或提交工作区根目录的 XAPK。
- 游戏源文件默认位于 `Assets/_Game/`；工程级配置、制作管理文档和隔离工具可使用主计划明确列出的根级目录。
- 不直接编辑 `Library/`、`Temp/`、`Logs/`、`Builds/`、`Artifacts/` 或 `UserSettings/` 中的生成内容。
- 严格遵守 `PROJECT_PLAN.md` 的 clean-room、版权和网络安全边界。

## Unity 工作规则

- Unity 采用 Visible Meta Files 与 Force Text；新增、移动或删除 `Assets/` 内容时必须保持 `.meta` 一致。
- 工程已经打开时，不启动会冲突的第二个 Editor 实例，也不强制关闭用户的 Unity。
- Package 变更必须固定版本，并同步提交 `Packages/manifest.json` 与 `Packages/packages-lock.json`。
- Codex 侧仅使用项目内 stdio MCP Server；Unity 内部桥接只允许 `127.0.0.1:6400` 回环通信，状态写入 `_ProjectTools/runtime/UnityMCPStatus`；禁止 LAN、远程 HTTP、遥测、全局配置写入、外部资产服务和 API Key。
- Unity MCP 使用项目内嵌入包 `Packages/com.coplaydev.unity-mcp`；上游设置窗口、自动更新和客户端配置器在隔离模式中被禁用。不得绕过隔离补丁或把包改回全局配置流程。
- 通过 Unity MCP 创建、加载或保存场景时只使用 `Assets/_Game/` 下的项目相对路径；不得传入绝对路径或 `..`。

## 依赖与下载

- 下载前必须先用中文告知用户具体组件、版本、来源、目标路径和原因。
- 所有便携工具、缓存和临时运行环境必须位于 `_ProjectTools/`；禁止系统级安装或修改系统 PATH。
- 第三方工具与包必须记录到 `Assets/_Game/Docs/ThirdPartyInventory.md`，包括版本、来源、许可证、用途、路径与校验信息。
- 内容资产及权属记录在 `StudioOps/ASSET_LEDGER.csv`；不要把工具依赖混入资产台账。

## Git 与验证

- 保持提交范围小且可解释；禁止使用破坏性回滚命令处理用户改动。
- `Library/`、构建、日志、证据和本地工具不得进入版本库。
- 报告完成前，按风险执行编译、EditMode/PlayMode 测试、场景冒烟或构建验证，并把证据写入既有验证文档或 `Artifacts/`。
- 未经验证不得声称“可用”“已修复”或“完成”；若存在限制，明确记录后再交接。
