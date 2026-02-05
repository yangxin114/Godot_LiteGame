# 内置玩家配置目录（res://plugins/workshop）

此目录用于存放项目内置的玩家配置（以 JSON 文件形式保存）。
这些文件随仓库提交，并会被 `Scripts/Customization/WorkshopLoader.cs` 读取。

加载规则（当前实现）
- 首先加载：res://plugins/workshop 中的所有 `*.json`（内置配置）。
- 然后加载：外部用户目录（默认为 `user://plugins/workshop`，可在场景中通过 `Player.WorkshopPath` 覆盖）的 `*.json`。
- 如果内置与外部有同名的 `PlayerData.Name`，外部（user://）会覆盖内置。

配置文件字段说明（与 `Customization.PlayerData` 对应）
- `Name` (string) — 玩家名称，必须填写以便参与覆盖匹配和显示。
- `Speed` (number) — 移动速度（像素/秒）。
- `Health` (integer) — 生命值。
- `SpritePath` (string) — 可选，贴图资源路径（推荐使用 `res://` 路径）。若为空可使用 `ColorHex` 创建占位。
- `ColorHex` (string) — 可选，十六进制颜色，如 `#RRGGBB` 或 `#RRGGBBAA`。
- `Abilities` (array of strings) — 可选，能力标识列表（由 `BasePlayer` 或工厂在运行时解释）。

示例（见同目录下的 `example_lightning.json` 与 `example_firewizard.json`）。

如何添加外部/外挂配置
- 将 JSON 文件放在 `user://plugins/workshop`（在 Windows 上通常映射到 Godot 项目的用户数据目录）或通过场景属性 `Player.WorkshopPath` 指向的其他目录。外部文件会覆盖同名内置配置。

注意
- `WorkshopLoader` 当前以 `Name` 字段作为唯一键做覆盖匹配；如果需要改为按文件名或其它字段匹配，请在 PR 中说明需求。
