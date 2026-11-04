# v0.6.2 卡面与角色美术审查（2026-09-30）

## 范围与结论

- 对 `HollowKnight/images/card_portraits/` 下的 **88 张实际卡面**逐张做 250×190 总览检查；四页索引图位于 `art_review/cards_1.jpg` 至 `cards_4.jpg`。`card.png` 是通用模板，`v3_preview_sheet.png` 是历史预览图，不计入 88 张。
- 原先仅有孤立图标、深色空背景、过小人物或重复剪影的卡面已替换。现在 88 张都有独立的场景插画，按卡名对应骨钉、灵魂、虚空、护盾、毒、织物等主题；每张均有 1000×760 大图和 250×190 小图。可用 `python tools/audit_art.py` 再生总览图并断言卡名、源图与尺寸一一对应。
- 原始插画源图在 `tools/generated_art_sources/cards/`，处理后的游戏贴图在 `HollowKnight/images/card_portraits/{big/,}`。源图以本地已有的《空洞骑士》游戏素材和角色形象作为视觉参考，通过 ImageGen 绘制新场景；**没有从 Wiki 新下载图片**。`tools/gen_cards_v5.py` 是未生成插画前的本地合成备选层；最终需在它之后运行 `tools/install_generated_art.py`，否则会覆盖这 88 张最终卡面。
- 角色选择封面 `HollowKnight/images/charui/knight_hero.png` 重绘为遗迹中的小骑士，选择按钮与锁定版由同一源图生成；初始遗物 `soul_catcher` 同步重画常规图、大图和白色轮廓。源图也在 `tools/generated_art_sources/`。

## 骨钉动作

- 原待机贴图自带背负的骨钉柄。`tools/gen_combat_body.py` 基于本地官方待机素材生成无背负骨钉柄的攻击身体贴图 `knight_combat_attack.png`。
- `KnightFeedback.Nail` 在出招时切到攻击身体贴图，六帧特效从背部起手位置移动到手部，再完成挥砍；结束、受击或新攻击打断时恢复待机贴图，避免背部骨钉与手中骨钉同时存在。静态七格对照图在 `art_review/combat_nail.jpg`。
- 本轮只完成代码构建与离线逐帧检查，仍需在 Steam 正常启动的游戏中确认攻击节奏、角色位置和多次连续攻击的实际观感。

## 本地验证

- `python tools/audit_art.py`：88 张源图与游戏贴图同名，大图 1000×760、小图 250×190，四张总览图均已检查。
- 指定 WSL 构建：**0 警告、0 错误**。美术完成时用户要求暂不上传；随后用户明确授权，v0.6.2 已于 2026-09-30 上传工坊物品 3796063046，并回下载核对 DLL/JSON/PCK/PDB 哈希。
- 本地 PCK 为 81,760,712 字节，二进制中可检索到 88 张大小卡面、攻击身体贴图、角色封面、初始遗物与 6 帧骨钉贴图对应的全部 185 个资源名；此项只验证资源名存在，未解析 PCK 内容。本地 mods 与 `release/workshop_upload/content/` 的 DLL/JSON/PCK/PDB SHA256 一致。
