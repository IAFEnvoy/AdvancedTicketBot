# Advanced Ticket Bot
这是一个可以高度自定义的Kook开票机器人，不需要修改代码，只需要修改配置json即可。

当然这个机器人只有一套模板，可以通过修改源代码来修改模板内容。

## 快速上手
1.安装`.NET`6.0或以上版本

2.从Release下载可执行程序

3.在程序根目录新建`main.json`和`ticket_info.json`并按照后文配置步骤进行配置。

4.**，启动！

## 配置
### `main.json`
**请记得删除注释**，json默认不识别注释
```json5
{
    "token":"xxx",//你的机器人token
    "admins":[]//有权呼出Card模式开票卡片的用户
}
```
### `ticket_info.json`
**请记得删除注释**，json默认不识别注释
```json5
[
  {
    //唯一id，用于标识频道所属类型
    "Id": "id",
    //0为卡片(Card)模式，1为指令(Command)模式
    "Type": 1,
    //Card：发送开票卡片指令。Command：触发开票指令。
    "Command": "/ticket",
    //Card：卡片标题。Command：开票标题
    "Name": "开票名称",
    //Card：子开票信息。Command：无作用
    "TicketInfos": [
      {
        //按钮内容
        "ButtonName": "",
        //按钮按下发送的返回值，不要重复
        "EventId": "",
        //开票成功卡片显示内容
        "Content": ""
      }
    ],
    //开票频道所属分组Id，填0表示和触发频道放一起，填1表示放外面
    "TicketCategoryId": 0,
    //Card：开票卡片备注。Command：开票成功卡片显示内容
    "Content": "开票提示内容",
    //Card：无作用。Command：可触发此指令的频道，填0表示不限制
    "TriggerChannel": 0,
    //是否在Ticket中At开票人
    "AtInTicket": true,
    //Card：无作用。Command：将at的人也拉入开票频道
    "AllowAt": true,
    //开票频道标题格式，可用占位符：%title%, %user_name%, %user_id%
    "TitleFormat": "%title% %user_name% %user_id%",
    //可执行关闭开票的身份组，留空表示不限制
    "CloseTicketRole": []
  }
]
```
## 正在使用此机器人的Kook服务器
[中国排位起床社区](https://kook.rbwcn.cn)

## 自行编译
安装`.NET`6.0或者更高版本SDK后使用Rider或者Visual Studio打开，点击`运行/编译`即可

## 贡献者
暂无