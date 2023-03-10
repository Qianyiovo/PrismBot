using Microsoft.EntityFrameworkCore;
using PrismBot.SDK.Data;
using PrismBot.SDK.Extensions;
using PrismBot.SDK.Interfaces;
using PrismBot.SDK.Models;
using Sora.EventArgs.SoraEvent;

namespace PrismBot.InternalPlugins.ElegantWhitelist.GroupCommands;

public class UserInformation : IGroupCommand
{
    public string GetCommand()
    {
        return "用户信息";
    }

    public string GetPermission()
    {
        return "ewl.userinfo";
    }

    public async Task OnPermissionDeniedAsync(string type, GroupMessageEventArgs eventArgs)
    {
        await eventArgs.SendDefaultPermissionDeniedMessageAsync();
    }

    public async Task OnPermissionGrantedAsync(string type, GroupMessageEventArgs eventArgs)
    {
        var args = eventArgs.Message.GetCommandArgs();
        //判断语法是否正确
        if (args.Length != 2)
        {
            await eventArgs.SourceGroup.SendGroupMessage("您输入的参数不符合要求。请参考以下语法进行输入：用户信息 <角色昵称/QQ/at>");
            return;
        }

        //获取玩家
        var db = new BotDbContext();
        Player? player = null;
        //判断有无at
        if (eventArgs.Message.GetAllAtList().Count() == 1)
            player = await db.Players.Include(x => x.Group).FirstOrDefaultAsync(x => x.QQ == eventArgs.Message.GetAllAtList().First());

        if (player == null)
        {
            //判断是否为QQ号
            if (long.TryParse(args[1], out var qq))
            {
                player = await db.Players.Include(x => x.Group).FirstOrDefaultAsync(x => x.QQ == qq) ?? await db.Players.Include(x => x.Group).FirstOrDefaultAsync(x => x.UserName == args[1]);
            }
            else
                //判断是否为角色昵称
                player = await db.Players.Include(x => x.Group).FirstOrDefaultAsync(x => x.UserName == args[1]);
        }

        if (player == null)
        {
            await eventArgs.SourceGroup.SendGroupMessage("未找到该用户。");
            return;
        }
        
        await eventArgs.SourceGroup.SendGroupMessage($"QQ：{player.QQ}\n" +
                                                     $"角色昵称：{player.UserName}\n" +
                                                     $"组别：{player.Group.GroupName}\n" +
                                                     $"硬币：{player.Coins}\n" +
                                                     $"是否被冻结：{(player.IsFreeze ? '是':'否')}\n" +
                                                     $"权限：{(player.GetPermissions().Any() ? string.Join(",", player.GetPermissions()) : "无")}\n" +
                                                     $"注册时间：{player.RegistrationTime}");
    }
}