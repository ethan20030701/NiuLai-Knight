# 创意工坊发布步骤（SteamCMD）

本目录已备好：
- `content/`            上传内容（HollowKnight.dll / .json / .pck 三件套）
- `HollowKnight_uploader.vdf`  steamcmd 上传配置
- `../workshop_preview.jpg`    工坊预览图（1920x1080）
- `../WORKSHOP_DESCRIPTION.md` 工坊描述文案（英文正文 + 变更日志）

## 第一次发布（约 10 分钟）

1. 下载 SteamCMD（若未安装）：
   https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
   解压到任意目录（例如 `C:\steamcmd\`）。

2. 在该目录打开命令行，登录你的 Steam 账号：
   steamcmd +login 你的Steam用户名
   （会要求输入密码和手机验证码）

3. 执行上传：
   steamcmd +login 你的Steam用户名 +workshop_build_item "C:\Users\asus\HollowKnight\release\workshop_upload\HollowKnight_uploader.vdf" +quit
   物品已创建：publishedfileid = 3796063046（本 vdf 已填好，以后重传直接更新此物品）。

4. 打开工坊物品后台（浏览器）：
   工坊页面：https://steamcommunity.com/sharedfiles/filedetails/?id=3796063046 编辑：
   - 标题：The Knight - Hollow Knight Character
   - 预览图：上传 release/workshop_preview.jpg
   - 描述：粘贴 WORKSHOP_DESCRIPTION.md 里的英文正文
   - 必需物品（Required items）：添加 BaseLib（工坊物品 ID 3737335127）
     —— 这一步最重要，订阅者会自动获得 BaseLib 前置
   - 可见性先保持"私密"，自己订阅测试一遍

5. 测试通过后，把可见性改为"公开"，发布完成。

## 以后更新版本

1. 构建 mod（WSL 里的 dotnet build）
2. 把新的 dll/json/pck 覆盖到 release/workshop_upload/content/
3. 修改 vdf 里的 publishedfileid 为你的物品 ID，changenote 写更新内容
4. 重新执行第 3 步的 steamcmd 命令

## 注意

- vdf 里 visibility "2" = 私密上传（首次发布安全），网页上改公开即可
- 若 steamcmd 报 "Not logged on"，先单独跑一次 steamcmd +login 完成验证
- 游戏本体（App 2868840）必须至少启动过一次，Steam 缓存才会正常
