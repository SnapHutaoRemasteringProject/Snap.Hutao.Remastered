// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Win32.Foundation;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service.Game.Island;

public struct IslandEnvironment
{
#pragma warning disable CS0649
    public IslandEnvironmentView View;
#pragma warning restore CS0649
    public BOOL IsOversea;
    public BOOL ProvideOffsets;

    public BOOL EnableSetFieldOfView;
    public float FieldOfView;
    public BOOL DisablePlayerPerspective;
    public BOOL DisableFog;
    public BOOL EnableSetTargetFrameRate;
    public int TargetFrameRate;
    public BOOL RemoveOpenTeamProgress;
    public BOOL HideQuestBanner;
    public BOOL DisableEventCameraMove;
    public BOOL DisableShowDamageText;
    public BOOL UsingTouchScreen;
    public BOOL RedirectCombineEntry;
    public BOOL ResinListItemId000106Allowed;
    public BOOL ResinListItemId000201Allowed;
    public BOOL ResinListItemId107009Allowed;
    public BOOL ResinListItemId107012Allowed;
    public BOOL ResinListItemId220007Allowed;

    public BOOL DisplayPaimon;
    public BOOL DebugMode;
    public BOOL HidePlayerInfo;
    public BOOL HideGrass;
    public BOOL GamepadHotSwitchEnabled;
    public BOOL EnableInLevelClockPageSpeedUp;
    public int CombineHotkey;
    public BOOL WeakMapCheck;
    public BOOL DisablePlayerDiveMosaic;
    public BOOL FastSkipTalk;
    public BOOL RedirectCatherine;
    public IslandHookOffsets HookOffsets;
}

public class HexStringToNintConverter : JsonConverter<nint>
{
    public override nint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? hexString = reader.GetString();
        if (hexString == null)
        {
            return 0;
        }

        if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hexString = hexString.Substring(2);
        }

        long longValue = long.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (nint)longValue;
    }

    public override void Write(Utf8JsonWriter writer, nint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{value:X}");
    }
}

public struct IslandHookOffsets
{
    // The order matches UnlockerIsland's native HookFunctionOffsets layout.
    public uint SetUid;
    public uint SetFov;
    public uint SetFog;
    public uint GetFps;
    public uint SetFps;
    public uint OpenTeam;
    public uint OpenTeamAdvanced;
    public uint CheckEnter;
    public uint QuestBanner;
    public uint FindObject;
    public uint ObjectActive;
    public uint CameraMove;
    public uint DamageText;
    public uint TouchInput;
    public uint KeyboardMouseInput;
    public uint JoypadInput;
    public uint CombineEntry;
    public uint CombineEntryPartner;
    public uint SetupResinList;
    public uint ResinList;
    public uint FindString;
    public uint PlayerPerspective;
    public uint IsObjectActive;
    public uint GameUpdate;
    public uint CatherineEntry;
    public uint SetupPlayerProfilePage;
    public uint SetWaterMaskUID;
    public uint FastSkipTalk;
    public uint SetText;
    public uint AvatarPaimonAppear;
    public uint GetComponent;
    public uint PlayerDiveMosaic;
    public uint GetName;
    public uint CheckCanOpenMap;
    public uint InLevelClockPageOkButtonClicked;
    public uint InLevelClockPageCloseButtonClicked;
    public uint ClosePage;
    public uint CatherineOpenDialog;
    public uint CatherineDialogCtor;
    public uint ExplorationEntry;
    public uint AvatarPaimonDisappear;

    public static readonly IslandHookOffsets Chinese = new()
    {
        SetUid = 0x1217AA90,
        SetFov = 0x019B0A00,
        SetFog = 0x01469F20,
        GetFps = 0x0146B6B0,
        SetFps = 0x0146B6C0,
        OpenTeam = 0x10A89B00,
        OpenTeamAdvanced = 0x10A83800,
        CheckEnter = 0x08A562C0,
        QuestBanner = 0x0F039540,
        FindObject = 0x01464800,
        ObjectActive = 0x01463FC0,
        CameraMove = 0x0C539020,
        DamageText = 0x142DB7B0,
        TouchInput = 0x0D484110,
        KeyboardMouseInput = 0x0D476E00,
        JoypadInput = 0x0D46DEF0,
        CombineEntry = 0x135572F0,
        CombineEntryPartner = 0x10EE8E10,
        SetupResinList = 0x106711E0,
        ResinList = 0x228,
        FindString = 0x004BBEC0,
        PlayerPerspective = 0x09DD2C90,
        IsObjectActive = 0x01463F90,
        GameUpdate = 0x19A4CC60,
        CatherineEntry = 0x0B930600,
        SetupPlayerProfilePage = 0x08F39150,
        SetWaterMaskUID = 0x107C2280,
        FastSkipTalk = 0x10C5E7A7,
        SetText = 0x1A30DD90,
        AvatarPaimonAppear = 0x13E81730,
        GetComponent = 0x01463870,
        PlayerDiveMosaic = 0x1055E51C,
        GetName = 0x01449820,
        CheckCanOpenMap = 0x13558130,
        InLevelClockPageOkButtonClicked = 0x0AE127B0,
        InLevelClockPageCloseButtonClicked = 0x09AAA620,
        ClosePage = 0x338,
        CatherineOpenDialog = 0x101AAC80,
        CatherineDialogCtor = 0x101B0C60,
        ExplorationEntry = 0x10EF9190,
        AvatarPaimonDisappear = 0x13E98480,
    };
}
