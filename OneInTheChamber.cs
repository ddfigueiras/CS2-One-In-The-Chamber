using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CSSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Cvars;


namespace OneInTheChamber;

public class OneInTheChamber : BasePlugin
{
    public override string ModuleAuthor => "ddfigueiras";
    public override string ModuleName => "OneInTheChamber";
    public override string ModuleVersion => "1.0";
    public const string pluginMsgTag = "[One In The Chamber]";
    public const int seconds = 30;
    public const int warnMessageTime = 5;
    public static bool oitc = false;
    private CSSTimer? _warnMessage;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerHurt>(EventPlayerHurt, HookMode.Pre);
        RegisterEventHandler<EventRoundEnd>(EventRoundEnd);
        RegisterEventHandler<EventItemEquip>(EventItemEquip);
        _warnMessage?.Kill();
    }
    [ConsoleCommand("oneinthechamber", "Ronda de hitkill!")]
    [ConsoleCommand("oinc", "Ronda de hitkill!")]
    [RequiresPermissions("@css/slay")]
    public void OnCommandOITCB(CCSPlayerController? caller, CommandInfo info)
    {
        if(caller == null) return;
        var players = Utilities.GetPlayers().Where(x => !x.IsBot);

        foreach (var player in players)
        {
            RemoveWeapons(player);
        }
        oitc = true;
        FriendlyFire(true);
        _warnMessage = new CSSTimer(seconds, () => announcePre());
    }
    [ConsoleCommand("setoneinthechamber", "Ronda de hitkill!")]
    [ConsoleCommand("setoinc", "Ronda de hitkill!")]
    [RequiresPermissions("@css/slay")]
    public void OnCommandSetOITCB(CCSPlayerController? caller, CommandInfo info)
    {
        if(caller == null) return;
        var players = Utilities.GetPlayers().Where(x => !x.IsBot);

        foreach (var player in players)
        {
            RemoveWeapons(player);
            player.GiveNamedItem("weapon_deagle");
            player.ExecuteClientCommand("slot2");
            deagleRefill(player);
            if(player.PlayerPawn.Value != null)
                player.PlayerPawn.Value.Health = 100;
        }
        oitc = true;
        FriendlyFire(true);
        Server.PrintToChatAll($" {ChatColors.Darkred} {pluginMsgTag} {ChatColors.Green}One in the chamber is activated!");
    }
    private void RefreshUI(CCSPlayerPawn player)
    {
        if(player.WeaponServices == null | player.ItemServices == null) return;

        string healthShot = "weapon_healthshot";
        if(player.ItemServices != null)
            VirtualFunctions.GiveNamedItem(player.ItemServices.Handle, healthShot, 0, 0, 0, 0);
        if(player.WeaponServices == null) return; 
        foreach (var weapon in player.WeaponServices.MyWeapons)
        {
            if(weapon != null && weapon.IsValid == true && weapon.Value != null && weapon.Value.IsValid == true && string.IsNullOrWhiteSpace(weapon.Value.DesignerName) == false && weapon.Value.DesignerName.Equals(healthShot))
            {
                weapon.Value.Remove();
                break;
            }
        }
    }
    public void RemoveWeapons(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }
        var weaponService = player.PlayerPawn.Value.WeaponServices;
        if (weaponService == null) return;
        weaponService.MyWeapons.Where(weapon => weapon.IsValid && weapon.Value.IsValid && !weapon.Value.DesignerName.Contains("knife")).ToList().ForEach(weapon => weapon.Value.Remove());
    }
    private HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        oitc = false;
        _warnMessage?.Kill();
        return HookResult.Continue;
    }
    private HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!@event.Userid.IsValid)
        {
            return HookResult.Continue;
        }
        if (!oitc || @event.Userid.Connected != PlayerConnectedState.PlayerConnected || !@event.Userid.PlayerPawn.IsValid || @event.Userid.PlayerPawn.Value == null)
        {
            return HookResult.Continue;
        }
        CCSPlayerController player = @event.Userid;
        CCSPlayerController atacker = @event.Attacker;

        if(@event.Weapon.Contains("deagle"))
            player.PlayerPawn.Value.Health = 100;
        if(atacker != null && atacker.IsValid && !atacker.IsBot)
        {
            player.PlayerPawn.Value.Health = 0;
            deagleRefill(atacker);
        }
        RefreshUI(player.PlayerPawn.Value);

        return HookResult.Continue;
    }
    public void FriendlyFire(bool active)
    {
        #pragma warning disable CS8602
        if(active)   
        {
            ConVar.Find("mp_teammates_are_enemies").SetValue(true);
            ConVar.Find("mp_friendlyfire").SetValue(true);
        }
        else 
        {
            ConVar.Find("mp_teammates_are_enemies").SetValue(false);
            ConVar.Find("mp_friendlyfire").SetValue(false);
        }
        #pragma warning restore CS8602
    }
    public void announcePre()
    {
        Server.PrintToChatAll($" {ChatColors.Darkred} {pluginMsgTag} {ChatColors.Green}One in the chamber starting in {warnMessageTime} seconds. Ready?!");
        _warnMessage = new CSSTimer(5, () => startoitc());
    }
    public static void deagleRefill(CCSPlayerController player)
    {
        if(player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null) return;
        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is { IsValid: true, Value.IsValid: true } && weapon.Value.DesignerName.Contains("weapon_deagle"))
            {
                weapon.Value.Clip1 = 1;
                weapon.Value.ReserveAmmo[0] = 0;
                Schema.SetSchemaValue<int>(weapon.Value.Handle, "CBasePlayerWeapon", "m_iClip1", 1);
                Schema.SetSchemaValue<int>(weapon.Value.Handle, "CBasePlayerWeapon", "m_iClip2", 0);
                Schema.SetSchemaValue<int>(weapon.Value.Handle, "CBasePlayerWeapon", "m_pReserveAmmo", 0);
            }
        }
    }
    public void startoitc()
    {
        var players = Utilities.GetPlayers().Where(player => player is { IsValid: true, PawnIsAlive: true, IsBot: false});
        foreach(var player in players)
        {
            if(player.PlayerPawn.Value != null)
            {
                player.PlayerPawn.Value.Health = 100;
            }
        }
        foreach(var player in players)
        {
            if(player.PlayerPawn.Value != null)
            {
                player.GiveNamedItem("weapon_deagle");
                player.ExecuteClientCommand("slot2");
                deagleRefill(player);
            }
        }
    }
    private HookResult EventItemEquip(EventItemEquip @event, GameEventInfo info) 
    {
        if(@event.Userid == null) return HookResult.Continue;
        CCSPlayerController? player = @event.Userid;
        if(oitc)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if(pawn == null) return HookResult.Continue;
            if(pawn.WeaponServices?.MyWeapons == null) return HookResult.Continue;

            foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon.Value == null || weapon.Value.DesignerName == null)
                    continue;
                if(!weapon.Value.DesignerName.Contains("deagle") && !weapon.Value.DesignerName.Contains("knife"))
                {
                    player.DropActiveWeapon();
                }
            }    
        }
        return HookResult.Continue;
    }
}