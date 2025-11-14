// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.Setting;

internal static class SettingKeys
{
    // Application
    public const string DataDirectory                   = "Snap::Hutao::Application::DataFolderPath";
    public const string OverrideElevationRequirement    = "Snap::Hutao::Application::Elevation::Override";
    public const string LaunchTimes                     = "Snap::Hutao::Application::LaunchTimes";
    public const string PreviousDataDirectoryToDelete   = "Snap::Hutao::Application::PreviousDataFolderToDelete";
    public const string LastVersion                     = "Snap::Hutao::Application::Update::LastVersion";
    public const string AlwaysIsFirstRunAfterUpdate     = "Snap::Hutao::Application::Update::LastVersion::TreatAsFirstRun";
    public const string OverrideUpdateVersionComparison = "Snap::Hutao::Application::Update::VersionComparison::Override";

    // Globalization
    public const string FirstDayOfWeek               = "Snap::Hutao::Globalization::FirstDayOfWeek";
    public const string PrimaryLanguage              = "Snap::Hutao::Globalization::PrimaryLanguage";
    public const string AnnouncementRegion           = "Snap::Hutao::Globalization::Region::Announcement";

    // UI
    public const string BackgroundImageType          = "Snap::Hutao::UI::BackgroundImage::Type";
    public const string ElementTheme                 = "Snap::Hutao::UI::ElementTheme";
    public const string SystemBackdropType           = "Snap::Hutao::UI::SystemBackdropType";
    public const string GuideState                   = "Snap::Hutao::UI::Windowing::GuideWindow::State::1.17";
    public const string LastWindowCloseBehavior      = "Snap::Hutao::UI::Windowing::LastWindowCloseBehavior";
    public const string IsLastWindowCloseBehaviorSet = "Snap::Hutao::UI::Windowing::LastWindowCloseBehavior::Set";
    public const string IsNavPaneOpen                = "Snap::Hutao::UI::Windowing::MainWindow::NavigationView::IsPaneOpen";

    // HomeCard
    public const string HomeCardAchievementOrder           = "Snap::Hutao::UI::Home::Card::Achievement::Order";
    public const string IsHomeCardAchievementPresented     = "Snap::Hutao::UI::Home::Card::Achievement::Presented";
    public const string HomeCardCalendarOrder              = "Snap::Hutao::UI::Home::Card::Calendar::Order";
    public const string IsHomeCardCalendarPresented        = "Snap::Hutao::UI::Home::Card::Calendar::Presented";
    public const string CalendarServerTimeZoneOffset       = "Snap::Hutao::UI::Home::Card::Calendar::ServerTimeZoneOffset";
    public const string HomeCardDailyNoteOrder             = "Snap::Hutao::UI::Home::Card::DailyNote::Order";
    public const string IsHomeCardDailyNotePresented       = "Snap::Hutao::UI::Home::Card::DailyNote::Presented";
    public const string HomeCardGachaStatisticsOrder       = "Snap::Hutao::UI::Home::Card::GachaStatistics::Order";
    public const string IsHomeCardGachaStatisticsPresented = "Snap::Hutao::UI::Home::Card::GachaStatistics::Presented";
    public const string HomeCardLaunchGameOrder            = "Snap::Hutao::UI::Home::Card::LaunchGame::Order";
    public const string IsHomeCardLaunchGamePresented      = "Snap::Hutao::UI::Home::Card::LaunchGame::Presented";
    public const string HomeCardSignInOrder                = "Snap::Hutao::UI::Home::Card::SignIn::Order";
    public const string IsHomeCardSignInPresented          = "Snap::Hutao::UI::Home::Card::SignIn::Presented";

    // HotKey
    public const string HotKeyRepeatForeverInGameOnly            = "Snap::Hutao::HotKey::RepeatForever::InGameOnly";
    public const string HotKeyKeyPressRepeatForever              = "Snap::Hutao::HotKey::RepeatForever::KeyPress";
    public const string HotKeyMouseClickRepeatForever            = "Snap::Hutao::HotKey::RepeatForever::MouseClick";
    public const string LowLevelKeyboardWebView2VideoPlayPause   = "Snap::Hutao::HotKey::LowLevel::WebView2::Video::PlayPause";
    public const string LowLevelKeyboardWebView2VideoFastForward = "Snap::Hutao::HotKey::LowLevel::WebView2::Video::FastForward";
    public const string LowLevelKeyboardWebView2VideoRewind      = "Snap::Hutao::HotKey::LowLevel::WebView2::Video::Rewind";
    public const string LowLevelKeyboardWebView2Hide             = "Snap::Hutao::HotKey::LowLevel::WebView2::Hide";
    public const string LowLevelKeyboardOverlayHide              = "Snap::Hutao::HotKey::LowLevel::Overlay::Hide";

    // Passport
    public const string PassportRefreshToken = "Snap::Hutao::Passport::RefreshToken";
    public const string PassportUserName     = "Snap::Hutao::Passport::UserName";

    // AvatarProperty
    public const string AvatarPropertySortDescriptionKind = "Snap::Hutao::AvatarProperty::SortDescriptionKind";

    // Cultivation
    public const string CultivationAvatarLevelCurrent           = "Snap::Hutao::Cultivation::Avatar::Level::Current";
    public const string CultivationAvatarLevelTarget            = "Snap::Hutao::Cultivation::Avatar::Level::Target";
    public const string CultivationAvatarSkillACurrent          = "Snap::Hutao::Cultivation::Avatar::SkillA::Current";
    public const string CultivationAvatarSkillATarget           = "Snap::Hutao::Cultivation::Avatar::SkillA::Target";
    public const string CultivationAvatarSkillECurrent          = "Snap::Hutao::Cultivation::Avatar::SkillE::Current";
    public const string CultivationAvatarSkillETarget           = "Snap::Hutao::Cultivation::Avatar::SkillE::Target";
    public const string CultivationAvatarSkillQCurrent          = "Snap::Hutao::Cultivation::Avatar::SkillQ::Current";
    public const string CultivationAvatarSkillQTarget           = "Snap::Hutao::Cultivation::Avatar::SkillQ::Target";
    public const string CultivationWeapon70LevelCurrent         = "Snap::Hutao::Cultivation::Weapon70::Level::Current";
    public const string CultivationWeapon70LevelTarget          = "Snap::Hutao::Cultivation::Weapon70::Level::Target";
    public const string CultivationWeapon90LevelCurrent         = "Snap::Hutao::Cultivation::Weapon90::Level::Current";
    public const string CultivationWeapon90LevelTarget          = "Snap::Hutao::Cultivation::Weapon90::Level::Target";
    public const string ResinStatisticsSelectedDropDistribution = "Snap::Hutao::Cultivation::ResinStatistics::DropDistribution";

    // GachaLog
    public const string IsEmptyHistoryWishVisible   = "Snap::Hutao::GachaLog::HistoryWish::EmptyVisible";
    public const string IsUnobtainedWishItemVisible = "Snap::Hutao::GachaLog::UnobtainedItem::Visible";

    // DailyNote
    public const string DailyNoteIsAutoRefreshEnabled  = "Snap::Hutao::DailyNote::AutoRefresh::Enabled";
    public const string DailyNoteRefreshSeconds        = "Snap::Hutao::DailyNote::RefreshSeconds";
    public const string DailyNoteReminderNotify        = "Snap::Hutao::DailyNote::ReminderNotify";
    public const string DailyNoteSilentWhenPlayingGame = "Snap::Hutao::DailyNote::SilentWhenPlayingGame";
    public const string DailyNoteWebhookUrl            = "Snap::Hutao::DailyNote::Webhook::Url";

    // Geetest
    public const string GeetestCustomCompositeUrl = "Snap::Hutao::Geetest::CustomCompositeUrl";

    // Game
    public const string LaunchAspectRatios                               = "Snap::Hutao::Game::CommandLine::AspectRatios";
    public const string LaunchUsingHoyolabAccount                        = "Snap::Hutao::Game::CommandLine::AuthTicket";
    public const string LaunchIsBorderless                               = "Snap::Hutao::Game::CommandLine::Borderless";
    public const string LaunchAreCommandLineArgumentsEnabled             = "Snap::Hutao::Game::CommandLine::Enabled";
    public const string LaunchIsExclusive                                = "Snap::Hutao::Game::CommandLine::Exclusive";
    public const string LaunchIsFullScreen                               = "Snap::Hutao::Game::CommandLine::FullScreen";
    public const string LaunchMonitor                                    = "Snap::Hutao::Game::CommandLine::Monitor";
    public const string LaunchIsMonitorEnabled                           = "Snap::Hutao::Game::CommandLine::Monitor::Enabled";
    public const string LaunchPlatformType                               = "Snap::Hutao::Game::CommandLine::PlatformType";
    public const string LaunchIsPlatformTypeEnabled                      = "Snap::Hutao::Game::CommandLine::PlatformType::Enabled";
    public const string LaunchScreenHeight                               = "Snap::Hutao::Game::CommandLine::ScreenHeight";
    public const string LaunchIsScreenHeightEnabled                      = "Snap::Hutao::Game::CommandLine::ScreenHeight::Enabled";
    public const string LaunchScreenWidth                                = "Snap::Hutao::Game::CommandLine::ScreenWidth";
    public const string LaunchIsScreenWidthEnabled                       = "Snap::Hutao::Game::CommandLine::ScreenWidth::Enabled";
    public const string LaunchUsingStarwardPlayTimeStatistics            = "Snap::Hutao::Game::InterProcess::Starward::PlayTimeStatistics";
    public const string LaunchUsingBetterGenshinImpactAutomation         = "Snap::Hutao::Game::InterProcess::BetterGenshinImpact::Automation";
    public const string LaunchDisableShowDamageText                      = "Snap::Hutao::Game::Island::DamageText::Show";
    public const string LaunchIsIslandEnabled                            = "Snap::Hutao::Game::Island::Enabled";
    public const string LaunchDisableEventCameraMove                     = "Snap::Hutao::Game::Island::Event::CameraMove::Disabled";
    public const string LaunchTargetFov                                  = "Snap::Hutao::Game::Island::FieldOfView";
    public const string LaunchIsSetFieldOfViewEnabled                    = "Snap::Hutao::Game::Island::FieldOfView::Enabled";
    public const string LaunchFixLowFovScene                             = "Snap::Hutao::Game::Island::FieldOfView::FixLowFovScene";
    public const string LaunchDisableFogRendering                        = "Snap::Hutao::Game::Island::FieldOfView::DisableFogRendering";
    public const string LaunchIsSetTargetFrameRateEnabled                = "Snap::Hutao::Game::Island::FrameRate::Enabled";
    public const string LaunchTargetFps                                  = "Snap::Hutao::Game::Island::FrameRate";
    public const string LaunchUsingTouchScreen                           = "Snap::Hutao::Game::Island::InputDevice::TouchScreen";
    public const string LaunchForceUsingTouchScreen                      = "Snap::Hutao::Game::Island::InputDevice::TouchScreen::ForceWhenIntegratedTouchPresent";
    public const string LaunchRemoveOpenTeamProgress                     = "Snap::Hutao::Game::Island::OpenTeamProgress::Remove";
    public const string LaunchHideQuestBanner                            = "Snap::Hutao::Game::Island::QuestBanner::Hide";
    public const string LaunchResinListItemId000106Allowed               = "Snap::Hutao::Game::Island::Reward::000106";
    public const string LaunchResinListItemId000201Allowed               = "Snap::Hutao::Game::Island::Reward::000201";
    public const string LaunchResinListItemId107009Allowed               = "Snap::Hutao::Game::Island::Reward::107009";
    public const string LaunchResinListItemId107012Allowed               = "Snap::Hutao::Game::Island::Reward::107012";
    public const string LaunchResinListItemId220007Allowed               = "Snap::Hutao::Game::Island::Reward::220007";
    public const string LaunchRedirectCombineEntry                       = "Snap::Hutao::Game::Island::Synthesis::Redirect";
    public const string LaunchUsingOverlay                               = "Snap::Hutao::Game::Overlay";
    public const string LaunchOverlaySelectedCatalogId                   = "Snap::Hutao::Game::Overlay::CatalogId";
    public const string LaunchOverlayWindowIsVisible                     = "Snap::Hutao::Game::Overlay::Visible";
    public const string EnableBetaGameInstall                            = "Snap::Hutao::Game::Package::BetaGame::Enable";
    public const string LaunchOverridePackageConvertDirectoryPermissions = "Snap::Hutao::Game::Package::Convert::Directory::Permissions::Override";
    public const string DownloadSpeedLimitPerSecondInKiloByte            = "Snap::Hutao::Game::Package::DownloadSpeedLimitPerSecondInKiloByte";
    public const string OverridePhysicalDriverType                       = "Snap::Hutao::Game::Package::PhysicalDriver::Type::Override";
    public const string PhysicalDriverIsAlwaysSolidState                 = "Snap::Hutao::Game::Package::PhysicalDriver::Type::IsSolidState";
    public const string TreatPredownloadAsMain                           = "Snap::Hutao::Game::Package::Predownload::TreatAsMain";
    public const string LaunchGamePath                                   = "Snap::Hutao::Game::Path";
    public const string LaunchGamePathEntries                            = "Snap::Hutao::Game::Path::Entries";
    public const string LaunchIsWindowsHDREnabled                        = "Snap::Hutao::Game::Registry::WindowsHDR::Enabled";

    // Web
    public const string AlphaBuildUseCnPatchEndpoint            = "Snap::Hutao::Web::AlphaBuild::Endpoint::UseCNPatch";
    public const string AlphaBuildUseFjPatchEndpoint            = "Snap::Hutao::Web::AlphaBuild::Endpoint::UseFJPatch";
    public const string ExcludedAnnouncementIds                 = "Snap::Hutao::Web::Homa::ExcludedAnnouncementIds";
    public const string StaticResourceImageQuality              = "Snap::Hutao::Web::StaticResource::ImageQuality";
    public const string StaticResourceImageArchive              = "Snap::Hutao::Web::StaticResource::ImageArchive";
    public const string BridgeShareSaveType                     = "Snap::Hutao::Web::WebView::BridgeShare::SaveType";
    public const string CompactWebView2WindowInactiveOpacity    = "Snap::Hutao::Web::WebView::Compact::InactiveOpacity";
    public const string CompactWebView2WindowPreviousSourceUrl  = "Snap::Hutao::Web::WebView::Compact::PreviousSourceUrl";
    public const string WebView2VideoFastForwardOrRewindSeconds = "Snap::Hutao::Web::WebView::Video::FastForwardOrRewind::Seconds";
}