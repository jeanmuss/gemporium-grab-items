# Gemporium 取物 MOD

[English](README.md) · [下载](https://github.com/jeanmuss/gemporium-grab-items/releases)

Windows x64 Steam 版 Gemporium 的独立 BepInEx 插件，按 **F8** 打开或关闭。

## 功能

- 15 种宝石分为已擦亮的破碎、完整两类；古物保持未擦亮。
- 左键 +1，右键 +10，直接加入家中库存。
- 原生图标、英文物品名、中英文分类、搜索及每页 20 件分页。
- 宝石重量沿用游戏随机规则；古物不会自动点亮收集记录。
- 排除 Amber、DLC 专属条目、商店商品及剧情钥匙。
- 面板打开时暂停游戏输入及光标回中央逻辑，关闭后恢复。

## 安装

版本 **0.1.3（预发布）**。构建对应 Steam build 25046214、Unity 6000.0.67f1、BepInEx 5.4.23.6。

1. 关闭游戏并备份存档。
2. 单独安装[官方 BepInEx 5.4.23.6 Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6)。
3. 下载 Release 中的 **GemporiumGrabItems-0.1.3-BepInEx.zip**，不要下载自动生成的 Source code 作为安装包。
4. 解压到游戏根目录，确认路径为 `BepInEx/plugins/GemporiumGrabItems/GemporiumGrabItems.dll`。
5. 从 Steam 启动，载入店铺/家中场景后按 F8。

新增物品通过游戏正常保存流程持久化，不转换已有库存。使用家中库存，不修改矿区背包容量。

## 更新、卸载与验证

关闭游戏后备份并替换 DLL。卸载只需移除插件目录，已经保存的物品仍然保留。

编译和安装包检查通过；最新版尚无游戏内验收确认。请测试鼠标移动、左右键取物、关闭面板恢复操作，以及保存后重新载入。其他 MOD 及未来游戏版本的兼容性尚未验证。

采用 Harmony 运行时补丁，不替换磁盘中的游戏程序集。源码与安装包不包含游戏程序集、素材、存档或加载器。构建见 [BUILDING.zh-CN.md](BUILDING.zh-CN.md)，版本记录见 [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)。本 MOD 为非官方作品。
