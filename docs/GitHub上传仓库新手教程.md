# GitHub 上传仓库新手教程：从第一次上传到日常更新

适用环境：Windows。示例使用你的账号 `ethan20030701` 和仓库 `NiuLai-Knight`。

编写日期：2026 年 11 月 4 日。界面按钮以后可能调整，文中附有 GitHub 官方帮助链接。

**第一次学习，建议先看第 1～5 节，用 GitHub Desktop 完成一次练习。第 7 节的命令行可以以后再学。**

## 1. 先了解你这个项目目前的状态

你的云端仓库已经创建并上传成功：

[打开 NiuLai-Knight 仓库](https://github.com/ethan20030701/NiuLai-Knight)

这次已经完成的事情：

- 按你的选择，仓库保持 **Public（公开）**，其他人可以查看和下载。
- 上传了整理后的代码、素材、文档和发布包，约 637.5 MiB。
- 从 GitHub 重新下载过一份，1490 个清单条目的 SHA-256 校验全部通过。
- 按你的确认，删除了 5 处测试环境、缓存和上传临时副本。
- 此后本地项目可能按你的要求清理；要继续开发，请按第 4 节重新 Clone。云端副本不包含原工作区的私人资料和完整历史备份。

**这里有一个与你特别相关的情况：**

原文件夹 `C:\MyProject` 有自己的旧 Git 历史，里面曾记录过助手配置和个人相关文件。这次上传的是另外整理出来的项目快照，所以原文件夹与云端仓库并不是直接同步的一对仓库。

以后要继续修改这个 GitHub 项目，建议先把云端仓库 **Clone（克隆）到一个新文件夹**，在新文件夹里工作。不要直接把原文件夹的整个历史推送过去，也不要通过强制推送来解决两边历史不同的问题。

本文里的新工作目录示例是：

```text
C:\GitHubProjects\NiuLai-Knight
```

这是供你以后操作的示例位置，教程本身没有替你创建这个目录。

## 2. 这些英文按钮是什么意思？

| 英文 | 中文理解 | 你会在什么时候遇到 |
| --- | --- | --- |
| Repository / Repo | 仓库：项目文件和版本记录的集合 | 创建或打开项目时 |
| Local | 本地：你电脑上的文件 | 编辑项目时 |
| Remote | 远程：GitHub 上的那份仓库 | 上传、下载时 |
| Clone | 克隆：下载文件以及 Git 版本记录，建立本地仓库 | 第一次把仓库放到电脑上时 |
| Changes | 改动：新增、修改或删除的文件 | 保存文件后 |
| Commit | 提交：把选定改动记成一个本地版本 | 想保存一组改动时 |
| Push | 推送：把本地提交上传到远程仓库 | 要让 GitHub 收到新版本时 |
| Fetch | 检查远程有没有新提交 | 开始工作前 |
| Pull | 把远程的新提交取回并整合到本地 | 在网页或另一台电脑修改过时 |
| Branch | 分支：一条版本开发路线 | 本文先使用 `main` 主分支 |
| origin | 远程仓库的常用简称 | 在 Push origin 等按钮上 |
| README.md | 项目说明文档 | 打开仓库首页时 |

你平时最常用的流程是：

```text
打开正确的本地仓库
        ↓
修改文件并保存
        ↓
检查 Changes，选择这次要记录的文件
        ↓
Commit：记录到本地版本历史
        ↓
Push：把本地提交上传到 GitHub
        ↓
打开 GitHub 网页，确认文件已经更新
```

**只保存文件，还需要 Commit 和 Push；做完 Commit，还需要 Push 才能上传。**

## 3. 上传前先整理：哪些文件应该放进仓库？

通常可以上传：

- 自己的源代码，例如 `.cs`、`.py`。
- 项目需要的图片、音频、场景和本地化文件。
- 项目说明、开发文档，以及不含隐私的配置示例。

通常应当排除：

- 密码、Token、API Key、私钥等登录或服务凭据。
- `.env` 等可能含有秘密的文件，以及个人资料、聊天记录。
- 能重新生成的缓存、构建结果、下载的工具环境。
- 与项目无关的文件、大量重复压缩包、完整游戏安装文件。

对于你的项目，`HollowKnightCode/` 是代码，`HollowKnight/` 是素材和相关资源，`docs/` 是文档。助手的 `memory/`、`config/` 等目录不属于公开项目内容。

### `.gitignore` 是什么？

它是一张“哪些未跟踪文件不加入 Git”的规则表，通常放在仓库根目录。

下面只是示例。已有 `.gitignore` 时先阅读并补充，不要把原有规则全部覆盖：

```gitignore
# 构建缓存
.build/
.godot/
**/bin/
**/obj/
**/__pycache__/

# 本机配置和秘密
Directory.Build.props
.env
.env.*
!.env.example
*.user

# 本项目不公开的助手资料
/memory/
/config/
/.openclaw/
/.autoclaw/
/.opencode/
```

`!.env.example` 表示允许一个不含真实密钥的配置示例加入版本管理。

记住两个限制：**已经被 Git 跟踪的文件，不会因为后来加入 `.gitignore` 就自动停止跟踪；旧提交里已经保存的内容，也不会自动消失。** 如果曾经上传真实密钥，应先撤销或更换密钥，再处理历史记录。

参考：[GitHub 官方：忽略文件](https://docs.github.com/en/get-started/git-basics/ignoring-files)。

## 4. 推荐方法：使用 GitHub Desktop 上传和更新

GitHub Desktop 是 GitHub 的桌面软件，可以用按钮操作 Git。刚开始可以只使用这一种方法。

### 4.1 安装与登录

1. 打开 [GitHub Desktop 官方下载页](https://desktop.github.com/download/)。
2. 下载 Windows 版本并安装。
3. 打开后选择登录 GitHub.com，按提示在浏览器完成登录和授权。
4. 如果账号开启了双重验证，按提示完成验证。
5. 配置提交作者姓名和邮箱。邮箱可以使用 GitHub 提供的隐私邮箱，获取方法见第 7.3 节。

**提交作者信息用于显示“这个版本是谁提交的”；账号登录授权决定你能否上传。这是两项不同设置。**

### 4.2 把你现有的云端仓库克隆到电脑

你的仓库已经有文件，所以从 Clone 开始：

1. 在 Desktop 中打开 **File → Clone repository**。
2. 选择 **URL** 标签。
3. 输入仓库地址：

   ```text
   https://github.com/ethan20030701/NiuLai-Knight.git
   ```

4. 在 **Local path** 选择新的本地存放位置，例如：

   ```text
   C:\GitHubProjects\NiuLai-Knight
   ```

5. 点击 **Clone**，等待完成。
6. 确认左上角仓库是 `NiuLai-Knight`，当前分支是 `main`。

目标位置应当是不存在或为空的文件夹。不要选装有原项目的 `C:\MyProject`，也不要把两个 `.git` 文件夹合到一起。

参考：[GitHub 官方：在 Desktop 中克隆仓库](https://docs.github.com/en/desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop)。

### 4.3 做一次简单练习

先上传一个小文本文件，熟悉流程：

1. 在 Desktop 中选择 **Repository → Show in Explorer**，打开这个仓库的本地文件夹。
2. 在里面创建 `我的第一次上传.txt`。
3. 用记事本打开，写一句：`这是我第一次自己上传到 GitHub。`，然后保存。
4. 回到 Desktop，左侧 **Changes** 应当出现这个文件。
5. 点击文件名，看右侧内容是否正确。
6. 确认只勾选本次要上传的文件。
7. 在左下角 **Summary** 填写：`添加第一次上传练习`。
8. 点击 **Commit to main**。
9. 再点击顶部的 **Push origin**，等待上传完成。
10. 打开仓库网页，确认能看到这个文件并打开它。

如果用记事本新建文件，保存时留意文件类型和扩展名，避免把文件保存到仓库之外。

以后修改代码、添加图片，也遵循同样的“检查改动 → Commit → Push → 网页确认”流程。

参考：[GitHub 官方：检查并提交改动](https://docs.github.com/en/desktop/making-changes-in-a-branch/committing-and-reviewing-changes-to-your-project-in-github-desktop)。

### 4.4 第二天继续工作时

1. 打开 Desktop，检查当前仓库和分支。
2. 点击 **Fetch origin**。
3. 如果出现 **Pull origin**，在本地没有未处理改动时点击它，取回远程更新。
4. 然后修改、保存、检查、Commit、Push。

如果你在 GitHub 网页上改过文件，或换过电脑，这一步尤其有用。遇到冲突时先停下来阅读提示，保留自己的文件副本，再决定如何合并。

参考：[GitHub 官方：同步分支](https://docs.github.com/en/desktop/working-with-your-remote-repository-on-github-or-github-enterprise/syncing-your-branch-in-github-desktop)。

## 5. 将来想上传另一个新项目，怎么创建新仓库？

你的 `NiuLai-Knight` 已经存在，这一节留给以后的其他项目。

1. 登录 GitHub，打开 [创建仓库页面](https://github.com/new)。
2. **Owner** 选择自己的账号。
3. **Repository name** 填一个新名称，例如 `my-first-project`。
4. **Description** 可以写项目用途，也可以暂时留空。
5. 选择可见性：**Public** 允许其他人查看；**Private** 仅向有权限的人开放。个人资料备份通常选择 Private 更合适。
6. 对本文这种“先创建、再 Clone、最后复制文件”的新手流程，可以勾选添加 README，让仓库先有一个说明文件。
7. 点击 **Create repository**。
8. 用第 4 节的方法 Clone 新仓库。
9. 把准备公开或备份的项目文件复制到克隆目录，检查 `.gitignore` 和 Changes，再 Commit、Push。

复制旧项目时只复制经过检查的项目内容，尤其不要把旧项目隐藏的 `.git` 文件夹带进去。

如果以后你需要连同旧项目的完整 Git 历史一起迁移，那属于另一种迁移流程，需要先检查旧历史是否适合上传。

参考：[GitHub 官方：创建仓库](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)。

## 6. 少量小文件：可以直接用网页上传

适合上传几份文档、小图片，或者临时添加这个教程。

1. 登录后打开仓库网页，进入希望存放文件的目录。
2. 点击文件列表上方的 **Add file → Upload files**。
3. 拖入文件，或点击 **choose your files** 选择。
4. 等待上传，检查文件名、目录和内容。
5. 填写提交说明，例如 `添加 GitHub 使用教程`。
6. 根据界面选择提交到当前分支，或创建新分支。
7. 如果提交到当前分支，完成 **Commit changes**；如果选择新分支，按提示 **Propose changes**，再通过 Pull Request 把改动合并到 `main`。
8. 回到仓库文件列表，检查是否成功。

如果看到 Pull Request，它相当于一张“请把这批改动合并到目标分支”的申请单。分支受到保护时，可能必须走这个流程。

截至编写日，网页上传 **单个文件不超过 25 MiB，一次最多 100 个文件**。你项目里约 85.86 MiB 的 `.pck` 包就不适合用网页上传。网页上传也不会应用 `.gitattributes` 的处理逻辑；对你这个需要保持文件字节一致的快照，优先使用 Desktop 或 Git。

参考：[GitHub 官方：添加文件](https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository)。

## 7. 进阶参考：用 PowerShell 和 Git 命令上传

**如果 Desktop 已经够用，可以先跳过这一节。** 两种方式操作的是同一种 Git 仓库，不需要各上传一遍。

下面的命令适用于“从 GitHub 克隆出来的新工作目录”。不要直接在原来的 `C:\MyProject` 中照抄执行。

命令一段一段运行，看到错误先停下来处理，不要继续执行后面的命令。

### 7.1 确认 Git 已安装

打开 PowerShell，输入：

```powershell
git --version
```

出现版本号就可以继续。如果提示找不到 `git`，从 [Git 官方 Windows 下载页](https://git-scm.com/install/windows) 安装，安装后重新打开 PowerShell。

### 7.2 克隆你的仓库

**如果第 4 节已经 Clone 过，不要再运行下面的克隆命令，直接进入现有克隆目录。**

如果还没 Clone，可以运行：

```powershell
New-Item -ItemType Directory -Force -Path "C:\GitHubProjects"
git clone https://github.com/ethan20030701/NiuLai-Knight.git "C:\GitHubProjects\NiuLai-Knight"
```

完成后进入目录，检查位置和远程地址：

```powershell
Set-Location "C:\GitHubProjects\NiuLai-Knight"
Get-Location
git remote -v
git status
```

`git remote -v` 应显示 `ethan20030701/NiuLai-Knight.git`。`git status` 可以告诉你所在分支，以及哪些改动尚未提交。

### 7.3 第一次提交前，设置作者信息

先看看这个仓库当前使用的姓名和邮箱：

```powershell
git config user.name
git config user.email
```

如果已有正确设置，就不用改。如果没有，可只为当前仓库设置姓名：

```powershell
git config user.name "ethan20030701"
```

邮箱建议使用 GitHub 提供的隐私邮箱：登录 GitHub → 头像 → **Settings → Emails**，找到 `noreply` 邮箱并复制完整地址。

下面示例中的 `YOUR_NOREPLY_EMAIL` 是占位符，**必须替换为你刚复制的实际邮箱，再执行**：

```powershell
git config user.email "YOUR_NOREPLY_EMAIL"
```

这里不使用 `--global`，所以只配置当前仓库。姓名和邮箱会写入提交记录；这些设置不会替你登录 GitHub。

参考：[设置 Git 提交姓名](https://docs.github.com/en/get-started/git-basics/setting-your-username-in-git)、[设置提交邮箱](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address)。

### 7.4 修改文件，并检查改动

在克隆目录中编辑文件、保存，然后运行：

```powershell
git status
git diff
```

`git status` 列出文件状态；`git diff` 显示尚未暂存的已跟踪文件改动。**新建的未跟踪文件，通常不会直接出现在 `git diff` 中，要另外打开查看。**

如果查看长内容时出现 `(END)`，按键盘上的 `q` 就能退出查看界面，回到输入命令的位置。

如果显示一大堆不认识的文件，先检查当前目录和 `.gitignore`。

### 7.5 选择这次要提交的文件

新手可以先明确指定一两个文件。例如，假设这次修改的是 `README.md`：

```powershell
git add "README.md"
git diff --cached --stat
git diff --cached
```

`git add` 把指定文件的当前改动放进“暂存区”，也就是本次提交的候选清单；`--cached` 用来检查这份清单。

当你确认目录里所有改动都应该进入本次提交时，也可以使用：

```powershell
git add .
git status
git diff --cached --stat
```

`git add .` 会包含当前目录及子目录里未被忽略的新增、修改和删除。运行前要检查上传范围，运行后还要检查暂存清单。

### 7.6 提交，再上传

假设检查无误：

```powershell
git commit -m "更新项目说明"
git push origin main
```

提交说明可以换成你实际做的事，例如“修复卡牌描述”“添加新的音效”。

本次 `clone` 已经建立了远程和分支关联，所以这里不需要再 `git init` 或 `git remote add origin`。

如果要求认证，按 Git Credential Manager 的提示在浏览器中登录、授权。完成后，再打开 GitHub 网页检查文件和最新提交。

### 7.7 后续每天的简化流程

开始修改之前，在**没有未处理本地改动**的情况下：

```powershell
git status
git pull --ff-only origin main
```

`--ff-only` 表示只接受无需合并的直接更新。如果提示无法 fast-forward，就停下来检查两边历史，不要随手改成强制覆盖。

修改并保存之后，重复：

```powershell
git status
git diff
git add "实际修改的文件路径"
git diff --cached --stat
git commit -m "描述本次修改"
git push origin main
```

其中的文件路径和提交说明要换成你自己的内容。

## 8. 为什么上传时不一定需要再次输入账号密码？

GitHub 的 HTTPS Git 操作通常通过 Token 或授权工具认证。Git Credential Manager 可以让你在浏览器完成登录、授权，并保存后续可复用的凭据；GitHub 账号密码不能直接作为 HTTPS Git 操作的密码使用。

这次你的电脑已有 `ethan20030701` 的 Git 登录授权，所以 Git 上传时使用了已有凭据。操作过程中没有读取或索取你的密码或 Token。

需要区分：

- 浏览器能打开公开仓库：通常只是在读取公开内容。
- 向你的仓库推送：需要这个仓库的写入权限。
- Git 里设置 `user.name` 和 `user.email`：只是标记提交作者，不会获得写入权限。
- 更换电脑、授权失效或撤销授权后：通常需要重新认证。

不要把密码或 Token 写进仓库地址、代码、截图或教程里。遇到登录问题，优先使用官方登录流程，而不是把凭据交给别人。

参考：[GitHub 官方：缓存 Git 凭据](https://docs.github.com/en/get-started/git-basics/caching-your-github-credentials-in-git)、[GitHub 官方：身份认证方式](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/about-authentication-to-github)。

## 9. 常见问题：看到这些提示怎么办？

### 9.1 `nothing to commit, working tree clean`

表示当前没有待提交的文件改动。它本身不能证明最近的提交已经上传。

如果刚做过 Commit，还应检查 Push 是否完成，并到 GitHub 网页确认。

### 9.2 `not a git repository`

当前目录不是 Git 仓库，也可能是终端进错目录。先运行 `Get-Location`，再进入正确的克隆目录。不要为了消除报错就在任意位置运行 `git init`。

### 9.3 `destination path ... already exists and is not an empty directory`

你选择的克隆目录已经有东西。检查它是不是之前已克隆的仓库；如果是，直接使用它。如果不是，选择另一个新的空目录，不要先删除不认识的文件。

### 9.4 `Authentication failed`、`Permission denied` 或 `403`

检查仓库地址、登录账号和写入权限。公开仓库可下载，并不代表任意账号都能推送。

Desktop 用户检查软件中登录的账号；命令行用户按凭据管理器的提示重新认证。不要用 GitHub 账号密码反复尝试 HTTPS Git 登录。

### 9.5 `rejected`、`non-fast-forward` 或 `fetch first`

通常表示 GitHub 上有你本地没有的提交，或双方历史不同。先保留本地改动、检查远程更新。新手遇到合并冲突时，记下完整报错再求助。

你的原项目与新快照有独立历史，因此尤其不能把 `git push --force` 当作通用修复办法。

### 9.6 提示文件太大

截至编写日，普通 Git 上传中：超过 **50 MiB** 的单个文件会触发警告；超过 **100 MiB** 的文件会被阻止。需要保存更大文件时，应了解 Git LFS；给用户分发构建好的软件包，也可以考虑 Releases。

你的 `.pck` 约 85.86 MiB，之前已成功上传，但反复提交大二进制包会让历史越来越大。以后可以让代码和素材留在仓库，把发布包放进 Releases。

如果大文件已经进入本地提交，仅再删文件、做一个新提交，通常仍无法消除旧提交中的大文件。保留文件副本后，再处理尚未推送的历史；不熟悉时先求助。

参考：[GitHub 官方：大文件限制](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)。

### 9.7 网络连接超时或被重置

先确认 GitHub 网页能否访问、网络是否稳定，以及本机是否需要使用现有代理。

如果电脑使用代理软件，地址和端口应以本机实际设置为准。不要照抄别人的代理地址。

初学时先用 Desktop 的登录和同步功能排查；不要为一次网络错误盲目修改全局 Git 配置。

## 10. 上传之后，怎样判断能不能删除本地文件？

先确认你需要保留哪些内容。**GitHub 仓库只包含实际提交并推送的内容**：被忽略的文件、未提交改动、本机环境、私人配置和外部依赖，不会因为仓库存在就自动得到备份。

建议按下面的顺序核对：

1. 在网页上打开仓库，确认分支、最新提交和关键文件。
2. 用另一个新目录 Clone，或通过网页 **Code → Download ZIP** 下载后解压检查。
3. 打开实际需要的代码、文档、图片和发布包；重要二进制文件可以比较 SHA-256。
4. 如果以后还要继续编译或运行，确认依赖安装和配置方法也有记录。文件完整不代表新电脑已经具备运行环境。
5. 单独检查 `.gitignore` 忽略的文件及仓库外部文件，确认其中有没有唯一副本。
6. 最后再决定删除范围。重要资料可以再保留一份独立备份。

`Download ZIP` 适合拿到文件；`Clone` 还会建立 Git 仓库，更适合继续开发。使用 Git LFS、子模块的项目还需要核对相应内容是否取得完整。

### 比较一个重要文件的 SHA-256

下面两行中的路径是占位示例，替换为实际的原文件和下载文件位置：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath "原文件的完整路径"
Get-FileHash -Algorithm SHA256 -LiteralPath "下载后文件的完整路径"
```

两份文件的 Hash 值相同，表示它们的内容一致。检查文本时还要留意 Git 的换行转换可能使字节级 Hash 不同；本次项目快照专门设置了保留原始字节。

你的仓库中的 `BACKUP_MANIFEST.json` 记录的是 **2026 年 11 月 4 日最初上传的项目快照，共 1490 个清单条目**，不包含后来添加的本教程。后续编辑文件不会自动更新它；以后验证新版本时，需要重新生成对应清单。

原目录中的个人资料、原始 Git 历史、部分旧备份没有上传。清理这些内容意味着放弃对应本地记录；GitHub 只保留已经提交的项目快照。

## 11. 现在就可以做的小练习

按第 4 节克隆仓库后，试着完成这三次操作：

1. 新建 `我的第一次上传.txt`，Commit、Push，再去网页打开。
2. 给这个文件加第二行，重新 Commit、Push，查看网页上的新内容和提交记录。
3. 在网页上改一句话，然后回到 Desktop，Fetch、Pull，确认本地也更新。

完成后，你就练习过了新增、更新、上传和取回四件最常用的事情。

**这份教程已保存在 GitHub 的 `docs/` 目录中。** 你可以在网页阅读，也可以 Clone 仓库后在本地打开。

## 12. 一页速查

| 想做什么 | GitHub Desktop 操作 | Git 命令参考 |
| --- | --- | --- |
| 第一次取回项目 | File → Clone repository | `git clone 仓库地址` |
| 查看自己改了什么 | Changes | `git status`、`git diff` |
| 选择本次提交文件 | 勾选 Changes 中的文件 | `git add "文件路径"` |
| 保存一个本地版本 | Commit to main | `git commit -m "说明"` |
| 把版本上传到 GitHub | Push origin | `git push origin main` |
| 检查远程更新 | Fetch origin | `git fetch origin` |
| 取回远程新版本 | Pull origin | 本文使用 `git pull --ff-only origin main` |
| 确认上传结果 | 打开仓库网页查看文件和提交 | 也可以另找一个空目录 Clone 后检查 |

日常上传记住：**选对仓库 → 检查改动 → Commit → Push → 网页确认。**
