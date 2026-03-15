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
    public int Reversed1;
    public int Reversed2;

#pragma warning disable CS0649
    public HookFunctionOffsets Offsets;
#pragma warning restore CS0649
}

public struct HookFunctionOffsets
{
#pragma warning disable CS0649
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetUid;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetFov;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetFog;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetFps;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetFps;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint OpenTeam;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint OpenTeamAdvanced;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint CheckEnter;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint QuestBanner;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint FindObject;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint ObjectActive;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint CameraMove;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint DamageText;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint TouchInput;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint KeyboardMouseInput;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint JoypadInput;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint CombineEntry;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint CombineEntryPartner;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetupResinList;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint ResinList;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint FindString;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint PlayerPerspective;

    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint IsObjectActive;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GameUpdate;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetPlayerID;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint SetText;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint MonoInLevelPlayerProfilePageV3Ctor;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetPlayerName;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint ActorManagerCtor;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetGlobalActor;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint AvatarPaimonAppear;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetComponent;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetText;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint GetName;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint CheckCanOpenMap;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint InLevelClockPageOkButtonClicked;
    [JsonConverter(typeof(HexStringToNintConverter))]
    public uint InLevelClockPageCloseButtonClicked;
#pragma warning restore CS0649
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
