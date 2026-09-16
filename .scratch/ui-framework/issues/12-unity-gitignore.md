Type: task
Status: resolved
Blocked by: none

## Question

在仓库根新增标准 Unity 版 `.gitignore`（GitHub 官方 Unity.gitignore 模板：Library/、Temp/、Logs/、obj/、UserSettings/、*.csproj、*.sln、.vs/、crash 报告等）。

保留 `.agents/` 与 `.scratch/` 纳入跟踪（项目工具与本 effort 票据）。

验收：`git status` 不再列出 Library/Temp/Logs 等生成目录。

## Answer

已在仓库根写入 GitHub 官方 Unity.gitignore 模板（Library/Temp/Obj/Build/Logs/UserSettings、VS/Rider/Gradle 缓存、csproj/sln、崩溃报告、打包产物、Addressables 临时产物等）。`.agents/` 与 `.scratch/` 未被排除，保持跟踪。
