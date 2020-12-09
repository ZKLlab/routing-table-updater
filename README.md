**路由表自动更新程序**

---

适用对象：

- 有两个网卡，一个连接宽带或宿舍局域网，一个连接学校 Wi-Fi，想同时实现高速访问外网和访问学校内网资源；
- 使用 Windows，安装了 Visual Studio。

假定：

- 连接宿舍局域网的网卡拥有更低的跃点数（使用 Powershell 命令 `Get-NetIPInterface` 查看，跃点数为 `InterfaceMetric` 字段，如果连接内网的跃点数反而更低，则此自动更新程序无效）；
- 连接学校 Wi-Fi 的网卡叫 `WLAN`（可修改）；
- 设备在上海大学。

使用方法：

1. 在 Visual Studio 中克隆存储库，存储库位置 `https://github.com/ZKLlab/routing-table-updater.git`；
2. “生成” -> “发布 RoutingTableUpdater”；
3. 在资源管理器打开项目目录，进入 `bin\Release\netcoreapp3.1\publish\`，找到 `RoutingTableUpdater.exe`；
4. 运行`taskschd.msc`，打开任务计划程序；
5. 点击“创建任务...”；
6. 在“常规”选项卡填写名称，勾选“使用最高权限运行”，如果不想在连接WiFi时看到命令提示符黑框，那么请将用户更改为`SYSTEM`；
7. 在“触发器”选项卡新建触发器，“开始任务”选择`发生事件时`，选择“基本”，“日志”选择`Microsoft-Windows-NetworkProfile/Operational`，“源”填写`NetworkProfile`，“事件”填写`10000`，点击“确定”；
8. 在“操作”选项卡新建操作，通过“浏览...”选择发布好的`RoutingTableUpdater.exe`程序，点击“确定”；
9. 点击“确定”完成创建，此后连接上 Wi-Fi 时会按照程序中设定的规则更新路由表。

高级操作：

- 修改`Program.cs` `Main`函数中的`interfaceName`变量选择连接学校内网的网卡名（如`以太网`）；
- 修改`Program.cs` `Main`函数中的`routingRules`变量设置要添加的路由规则；
- 修改完以后记得重新发布；
- 修改“组策略 -> 计算机管理 -> 管理模板 -> 网络 -> Windows 连接管理器 -> 最小化到 Internet 或 Windows 域的同时连接数”，启用该组策略，设置最小化策略选项为“0 = 允许同时连接”，实现连接上以太网以后还能自动连接Wi-Fi。

许可证：

- 随便用
