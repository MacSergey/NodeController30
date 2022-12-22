namespace NodeController
{
	public class Localize
	{
		public static System.Globalization.CultureInfo Culture {get; set;}
		public static ModsCommon.LocalizeManager LocaleManager {get;} = new ModsCommon.LocalizeManager("Localize", typeof(Localize).Assembly);

		/// <summary>
		/// This action cannot be undone.
		/// </summary>
		public static string MessageBox_CantUndone => LocaleManager.GetString("MessageBox_CantUndone", Culture);

		/// <summary>
		/// Original Node Controller data was imported.
		/// </summary>
		public static string Mod_BackwardCompatibilityMessage => LocaleManager.GetString("Mod_BackwardCompatibilityMessage", Culture);

		/// <summary>
		/// WARNING: If you make a new save, you will no longer be able to use original Node Controller on this 
		/// </summary>
		public static string Mod_BackwardCompatibilityWarning => LocaleManager.GetString("Mod_BackwardCompatibilityWarning", Culture);

		/// <summary>
		/// Control node type and shape
		/// </summary>
		public static string Mod_Description => LocaleManager.GetString("Mod_Description", Culture);

		/// <summary>
		/// Load was completed with errors.
		/// </summary>
		public static string Mod_LoadFailed => LocaleManager.GetString("Mod_LoadFailed", Culture);

		/// <summary>
		/// Load was completed with errors.
		/// </summary>
		public static string Mod_LoadFailedAll => LocaleManager.GetString("Mod_LoadFailedAll", Culture);

		/// <summary>
		/// [NEW] Added auto mode for main slope direction.
		/// </summary>
		public static string Mod_WhatsNewMessage3_0_1 => LocaleManager.GetString("Mod_WhatsNewMessage3_0_1", Culture);

		/// <summary>
		/// [FIXED] Fix node height outside available tiles (5x5).
		/// </summary>
		public static string Mod_WhatsNewMessage3_0_2 => LocaleManager.GetString("Mod_WhatsNewMessage3_0_2", Culture);

		/// <summary>
		/// [TRANSLATION] Added Portuguese translation.
		/// </summary>
		public static string Mod_WhatsNewMessage3_0_3 => LocaleManager.GetString("Mod_WhatsNewMessage3_0_3", Culture);

		/// <summary>
		/// [NEW] Added missing dependencies and mod conflict checker.
		/// </summary>
		public static string Mod_WhatsNewMessage3_0_4 => LocaleManager.GetString("Mod_WhatsNewMessage3_0_4", Culture);

		/// <summary>
		/// [FIXED] Fix segments collision calculation.
		/// </summary>
		public static string Mod_WhatsNewMessage3_0_5 => LocaleManager.GetString("Mod_WhatsNewMessage3_0_5", Culture);

		/// <summary>
		/// [NEW] Added underground mode (hold Shift to select underground node).
		/// </summary>
		public static string Mod_WhatsNewMessage3_1 => LocaleManager.GetString("Mod_WhatsNewMessage3_1", Culture);

		/// <summary>
		/// [FIXED]  Fixed not updating nodes after loading map.
		/// </summary>
		public static string Mod_WhatsNewMessage3_1_1 => LocaleManager.GetString("Mod_WhatsNewMessage3_1_1", Culture);

		/// <summary>
		/// [TRANSLATION] Added Indonesian, Korean and Malay translations.
		/// </summary>
		public static string Mod_WhatsNewMessage3_1_2 => LocaleManager.GetString("Mod_WhatsNewMessage3_1_2", Culture);

		/// <summary>
		/// [NEW] Added colored text to tooltips.
		/// </summary>
		public static string Mod_WhatsNewMessage3_1_3 => LocaleManager.GetString("Mod_WhatsNewMessage3_1_3", Culture);

		/// <summary>
		/// [NEW] Added the ability to edit decorative networks. Max slope and twist value for decoration networ
		/// </summary>
		public static string Mod_WhatsNewMessage3_2 => LocaleManager.GetString("Mod_WhatsNewMessage3_2", Culture);

		/// <summary>
		/// [UPDATED] Added the ability to disable option "fix stuck pedestrian on long intersections" due to co
		/// </summary>
		public static string Mod_WhatsNewMessage3_3 => LocaleManager.GetString("Mod_WhatsNewMessage3_3", Culture);

		/// <summary>
		/// [UPDATED] Added Plazas & Promenades DLC support.
		/// </summary>
		public static string Mod_WhatsNewMessage3_3_1 => LocaleManager.GetString("Mod_WhatsNewMessage3_3_1", Culture);

		/// <summary>
		/// [FIXED] Fixed terrain glitches that could appear when using Surface Painter mod.
		/// </summary>
		public static string Mod_WhatsNewMessage3_3_2 => LocaleManager.GetString("Mod_WhatsNewMessage3_3_2", Culture);

		/// <summary>
		/// [FIXED] Fixed compatibility with Adaptive Network mod which could cause blue voids instead of nodes.
		/// </summary>
		public static string Mod_WhatsNewMessage3_3_3 => LocaleManager.GetString("Mod_WhatsNewMessage3_3_3", Culture);

		/// <summary>
		/// Bend
		/// </summary>
		public static string NodeStyle_Bend => LocaleManager.GetString("NodeStyle_Bend", Culture);

		/// <summary>
		/// Crossing
		/// </summary>
		public static string NodeStyle_Crossing => LocaleManager.GetString("NodeStyle_Crossing", Culture);

		/// <summary>
		/// Custom
		/// </summary>
		public static string NodeStyle_Custom => LocaleManager.GetString("NodeStyle_Custom", Culture);

		/// <summary>
		/// End
		/// </summary>
		public static string NodeStyle_End => LocaleManager.GetString("NodeStyle_End", Culture);

		/// <summary>
		/// Middle
		/// </summary>
		public static string NodeStyle_Middle => LocaleManager.GetString("NodeStyle_Middle", Culture);

		/// <summary>
		/// Stretched
		/// </summary>
		public static string NodeStyle_Stretch => LocaleManager.GetString("NodeStyle_Stretch", Culture);

		/// <summary>
		/// UTurn
		/// </summary>
		public static string NodeStyle_UTurn => LocaleManager.GetString("NodeStyle_UTurn", Culture);

		/// <summary>
		/// All
		/// </summary>
		public static string Options_All => LocaleManager.GetString("Options_All", Culture);

		/// <summary>
		/// Calculate shift by intersections
		/// </summary>
		public static string Option_CalculateShiftByIntersections => LocaleManager.GetString("Option_CalculateShiftByIntersections", Culture);

		/// <summary>
		/// Calculate shift by nearby nodes
		/// </summary>
		public static string Option_CalculateShiftByNearby => LocaleManager.GetString("Option_CalculateShiftByNearby", Culture);

		/// <summary>
		/// Calculate twist by intersections
		/// </summary>
		public static string Option_CalculateTwistByIntersections => LocaleManager.GetString("Option_CalculateTwistByIntersections", Culture);

		/// <summary>
		/// Calculate twist by nearby nodes
		/// </summary>
		public static string Option_CalculateTwistByNearby => LocaleManager.GetString("Option_CalculateTwistByNearby", Culture);

		/// <summary>
		/// Collision
		/// </summary>
		public static string Option_Collision => LocaleManager.GetString("Option_Collision", Culture);

		/// <summary>
		/// Reset offset
		/// </summary>
		public static string Option_KeepDefault => LocaleManager.GetString("Option_KeepDefault", Culture);

		/// <summary>
		/// Main slope direction
		/// </summary>
		public static string Option_MainSlopeDirection => LocaleManager.GetString("Option_MainSlopeDirection", Culture);

		/// <summary>
		/// Auto
		/// </summary>
		public static string Option_MainSlopeDirectionAuto => LocaleManager.GetString("Option_MainSlopeDirectionAuto", Culture);

		/// <summary>
		/// Manually
		/// </summary>
		public static string Option_MainSlopeDirectionManually => LocaleManager.GetString("Option_MainSlopeDirectionManually", Culture);

		/// <summary>
		/// Make ends straight
		/// </summary>
		public static string Option_MakeStraightEnds => LocaleManager.GetString("Option_MakeStraightEnds", Culture);

		/// <summary>
		/// Marking
		/// </summary>
		public static string Option_Marking => LocaleManager.GetString("Option_Marking", Culture);

		/// <summary>
		/// More options
		/// </summary>
		public static string Option_MoreOptions => LocaleManager.GetString("Option_MoreOptions", Culture);

		/// <summary>
		/// Nodeless
		/// </summary>
		public static string Option_NodeLess => LocaleManager.GetString("Option_NodeLess", Culture);

		/// <summary>
		/// Offset
		/// </summary>
		public static string Option_Offset => LocaleManager.GetString("Option_Offset", Culture);

		/// <summary>
		/// {0}m
		/// </summary>
		public static string Option_OffsetFormat => LocaleManager.GetString("Option_OffsetFormat", Culture);

		/// <summary>
		/// Reset to default
		/// </summary>
		public static string Option_ResetToDefault => LocaleManager.GetString("Option_ResetToDefault", Culture);

		/// <summary>
		/// Rotate
		/// </summary>
		public static string Option_Rotate => LocaleManager.GetString("Option_Rotate", Culture);

		/// <summary>
		/// {0}°
		/// </summary>
		public static string Option_RotateFormat => LocaleManager.GetString("Option_RotateFormat", Culture);

		/// <summary>
		/// Set shift between intersections
		/// </summary>
		public static string Option_SetShiftBetweenIntersections => LocaleManager.GetString("Option_SetShiftBetweenIntersections", Culture);

		/// <summary>
		/// Set twist between intersections
		/// </summary>
		public static string Option_SetTwistBetweenIntersections => LocaleManager.GetString("Option_SetTwistBetweenIntersections", Culture);

		/// <summary>
		/// Shift
		/// </summary>
		public static string Option_Shift => LocaleManager.GetString("Option_Shift", Culture);

		/// <summary>
		/// {0}m
		/// </summary>
		public static string Option_ShiftFormat => LocaleManager.GetString("Option_ShiftFormat", Culture);

		/// <summary>
		/// Slope
		/// </summary>
		public static string Option_Slope => LocaleManager.GetString("Option_Slope", Culture);

		/// <summary>
		/// {0}°
		/// </summary>
		public static string Option_SlopeFormat => LocaleManager.GetString("Option_SlopeFormat", Culture);

		/// <summary>
		/// Stretch
		/// </summary>
		public static string Option_Stretch => LocaleManager.GetString("Option_Stretch", Culture);

		/// <summary>
		/// {0}%
		/// </summary>
		public static string Option_StretchFormat => LocaleManager.GetString("Option_StretchFormat", Culture);

		/// <summary>
		/// Style
		/// </summary>
		public static string Option_Style => LocaleManager.GetString("Option_Style", Culture);

		/// <summary>
		/// Flat
		/// </summary>
		public static string Option_StyleFlat => LocaleManager.GetString("Option_StyleFlat", Culture);

		/// <summary>
		/// Slope
		/// </summary>
		public static string Option_StyleSlope => LocaleManager.GetString("Option_StyleSlope", Culture);

		/// <summary>
		/// Twist
		/// </summary>
		public static string Option_Twist => LocaleManager.GetString("Option_Twist", Culture);

		/// <summary>
		/// {0}°
		/// </summary>
		public static string Option_TwistFormat => LocaleManager.GetString("Option_TwistFormat", Culture);

		/// <summary>
		/// Node type
		/// </summary>
		public static string Option_Type => LocaleManager.GetString("Option_Type", Culture);

		/// <summary>
		/// Node #{0}
		/// </summary>
		public static string Panel_NodeId => LocaleManager.GetString("Panel_NodeId", Culture);

		/// <summary>
		/// Applies after game restart
		/// </summary>
		public static string Settings_ApplyAfterRestart => LocaleManager.GetString("Settings_ApplyAfterRestart", Culture);

		/// <summary>
		/// Backup
		/// </summary>
		public static string Settings_BackupTab => LocaleManager.GetString("Settings_BackupTab", Culture);

		/// <summary>
		/// Copy path to clipboard
		/// </summary>
		public static string Settings_CopyPathToClipboard => LocaleManager.GetString("Settings_CopyPathToClipboard", Culture);

		/// <summary>
		/// Delete data from all nodes
		/// </summary>
		public static string Settings_DeleteDataButton => LocaleManager.GetString("Settings_DeleteDataButton", Culture);

		/// <summary>
		/// Delete all data
		/// </summary>
		public static string Settings_DeleteDataCaption => LocaleManager.GetString("Settings_DeleteDataCaption", Culture);

		/// <summary>
		/// Do you really want to remove all data?
		/// </summary>
		public static string Settings_DeleteDataMessage => LocaleManager.GetString("Settings_DeleteDataMessage", Culture);

		/// <summary>
		/// Dump nodes data to file
		/// </summary>
		public static string Settings_DumpDataButton => LocaleManager.GetString("Settings_DumpDataButton", Culture);

		/// <summary>
		/// Dump nodes data
		/// </summary>
		public static string Settings_DumpDataCaption => LocaleManager.GetString("Settings_DumpDataCaption", Culture);

		/// <summary>
		/// Dump failed
		/// </summary>
		public static string Settings_DumpMessageFailed => LocaleManager.GetString("Settings_DumpMessageFailed", Culture);

		/// <summary>
		/// Dump successfully saved to file
		/// </summary>
		public static string Settings_DumpMessageSuccess => LocaleManager.GetString("Settings_DumpMessageSuccess", Culture);

		/// <summary>
		/// Insert node
		/// </summary>
		public static string Settings_InsertNode => LocaleManager.GetString("Settings_InsertNode", Culture);

		/// <summary>
		/// Disabled
		/// </summary>
		public static string Settings_InsertNodeDisabled => LocaleManager.GetString("Settings_InsertNodeDisabled", Culture);

		/// <summary>
		/// By click on segment
		/// </summary>
		public static string Settings_InsertNodeEnabled => LocaleManager.GetString("Settings_InsertNodeEnabled", Culture);

		/// <summary>
		/// By click with {0} on segment
		/// </summary>
		public static string Settings_InsertNodeWithModifier => LocaleManager.GetString("Settings_InsertNodeWithModifier", Culture);

		/// <summary>
		/// Fix stuck pedestrian on long intersections
		/// </summary>
		public static string Settings_LongIntersectionFix => LocaleManager.GetString("Settings_LongIntersectionFix", Culture);

		/// <summary>
		/// This option increase max pathfind distance on intersection, that prevent stuck pedestrians on such a
		/// </summary>
		public static string Settings_LongIntersectionFixWarning => LocaleManager.GetString("Settings_LongIntersectionFixWarning", Culture);

		/// <summary>
		/// Node is sloped by default
		/// </summary>
		public static string Settings_NodeIsSlopedByDefault => LocaleManager.GetString("Settings_NodeIsSlopedByDefault", Culture);

		/// <summary>
		/// Options visibility
		/// </summary>
		public static string Settings_OptionsVisibility => LocaleManager.GetString("Settings_OptionsVisibility", Culture);

		/// <summary>
		/// Disabled
		/// </summary>
		public static string Settings_Option_Disabled => LocaleManager.GetString("Settings_Option_Disabled", Culture);

		/// <summary>
		/// Сollapsed
		/// </summary>
		public static string Settings_Option_Hidden => LocaleManager.GetString("Settings_Option_Hidden", Culture);

		/// <summary>
		/// Always visible
		/// </summary>
		public static string Settings_Option_Visible => LocaleManager.GetString("Settings_Option_Visible", Culture);

		/// <summary>
		/// Show nearby nodes overlay
		/// </summary>
		public static string Settings_RenderNearNode => LocaleManager.GetString("Settings_RenderNearNode", Culture);

		/// <summary>
		/// Restore
		/// </summary>
		public static string Settings_Restore => LocaleManager.GetString("Settings_Restore", Culture);

		/// <summary>
		/// Restore nodes data from file
		/// </summary>
		public static string Settings_RestoreDataButton => LocaleManager.GetString("Settings_RestoreDataButton", Culture);

		/// <summary>
		/// Restore nodes data
		/// </summary>
		public static string Settings_RestoreDataCaption => LocaleManager.GetString("Settings_RestoreDataCaption", Culture);

		/// <summary>
		/// Do you really want to restore nodes data?
		/// </summary>
		public static string Settings_RestoreDataMessage => LocaleManager.GetString("Settings_RestoreDataMessage", Culture);

		/// <summary>
		/// Nodes data restore failed
		/// </summary>
		public static string Settings_RestoreDataMessageFailed => LocaleManager.GetString("Settings_RestoreDataMessageFailed", Culture);

		/// <summary>
		/// Nodes data successfully restored
		/// </summary>
		public static string Settings_RestoreDataMessageSuccess => LocaleManager.GetString("Settings_RestoreDataMessageSuccess", Culture);

		/// <summary>
		/// Select middle nodes
		/// </summary>
		public static string Settings_SelectMiddleNodes => LocaleManager.GetString("Settings_SelectMiddleNodes", Culture);

		/// <summary>
		/// If this option is disabled, the middle nodes will move to clicked position
		/// </summary>
		public static string Settings_SelectMiddleNodesDiscription => LocaleManager.GetString("Settings_SelectMiddleNodesDiscription", Culture);

		/// <summary>
		/// Enter underground mode
		/// </summary>
		public static string Settings_ShortcutEnterUnderground => LocaleManager.GetString("Settings_ShortcutEnterUnderground", Culture);

		/// <summary>
		/// Exit underground mode
		/// </summary>
		public static string Settings_ShortcutExitUnderground => LocaleManager.GetString("Settings_ShortcutExitUnderground", Culture);

		/// <summary>
		/// Toogle underground
		/// </summary>
		public static string Settings_ToggleUnderground => LocaleManager.GetString("Settings_ToggleUnderground", Culture);

		/// <summary>
		/// Press {0} to enter and {1} to exit
		/// </summary>
		public static string Settings_ToggleUndergroundButtons => LocaleManager.GetString("Settings_ToggleUndergroundButtons", Culture);

		/// <summary>
		/// Hold {0}
		/// </summary>
		public static string Settings_ToggleUndergroundHold => LocaleManager.GetString("Settings_ToggleUndergroundHold", Culture);

		/// <summary>
		/// Calculate shift by intersections
		/// </summary>
		public static string Setting_ShortcutCalculateShiftByIntersections => LocaleManager.GetString("Setting_ShortcutCalculateShiftByIntersections", Culture);

		/// <summary>
		/// Calculate shift by nearby nodes
		/// </summary>
		public static string Setting_ShortcutCalculateShiftByNearby => LocaleManager.GetString("Setting_ShortcutCalculateShiftByNearby", Culture);

		/// <summary>
		/// Calculate twist by intersections
		/// </summary>
		public static string Setting_ShortcutCalculateTwistByIntersections => LocaleManager.GetString("Setting_ShortcutCalculateTwistByIntersections", Culture);

		/// <summary>
		/// Calculate twist by nearby nodes
		/// </summary>
		public static string Setting_ShortcutCalculateTwistByNearby => LocaleManager.GetString("Setting_ShortcutCalculateTwistByNearby", Culture);

		/// <summary>
		/// Change main slope direction mode
		/// </summary>
		public static string Setting_ShortcutChangeMainRoadMode => LocaleManager.GetString("Setting_ShortcutChangeMainRoadMode", Culture);

		/// <summary>
		/// Change node style
		/// </summary>
		public static string Setting_ShortcutChangeNodeStyle => LocaleManager.GetString("Setting_ShortcutChangeNodeStyle", Culture);

		/// <summary>
		/// Reset offset
		/// </summary>
		public static string Setting_ShortcutKeepDefault => LocaleManager.GetString("Setting_ShortcutKeepDefault", Culture);

		/// <summary>
		/// Make ends straight
		/// </summary>
		public static string Setting_ShortcutMakeStraightEnds => LocaleManager.GetString("Setting_ShortcutMakeStraightEnds", Culture);

		/// <summary>
		/// Reset to default
		/// </summary>
		public static string Setting_ShortcutResetToDefault => LocaleManager.GetString("Setting_ShortcutResetToDefault", Culture);

		/// <summary>
		/// Set shift between intersections
		/// </summary>
		public static string Setting_ShortcutSetShiftBetweenIntersections => LocaleManager.GetString("Setting_ShortcutSetShiftBetweenIntersections", Culture);

		/// <summary>
		/// Set twist between intersections
		/// </summary>
		public static string Setting_ShortcutSetTwistBetweenIntersections => LocaleManager.GetString("Setting_ShortcutSetTwistBetweenIntersections", Culture);

		/// <summary>
		/// Press {0} to enter
		/// </summary>
		public static string Tool_EnterUnderground => LocaleManager.GetString("Tool_EnterUnderground", Culture);

		/// <summary>
		/// Press {0} to exit
		/// </summary>
		public static string Tool_ExitUnderground => LocaleManager.GetString("Tool_ExitUnderground", Culture);

		/// <summary>
		/// Hold {0} to alignment roads
		/// </summary>
		public static string Tool_InfoAlignMode => LocaleManager.GetString("Tool_InfoAlignMode", Culture);

		/// <summary>
		/// Click to align with this corner
		/// </summary>
		public static string Tool_InfoApplyAlign => LocaleManager.GetString("Tool_InfoApplyAlign", Culture);

		/// <summary>
		/// Hold {0} to change main slope direction
		/// </summary>
		public static string Tool_InfoChangeMainMode => LocaleManager.GetString("Tool_InfoChangeMainMode", Culture);

		/// <summary>
		/// Node #{0}
		/// </summary>
		public static string Tool_InfoClickNode => LocaleManager.GetString("Tool_InfoClickNode", Culture);

		/// <summary>
		/// Click to select corner you want to align
		/// </summary>
		public static string Tool_InfoClickToSelectFirstAlign => LocaleManager.GetString("Tool_InfoClickToSelectFirstAlign", Culture);

		/// <summary>
		/// Drag to change offset
		/// </summary>
		public static string Tool_InfoDragCenter => LocaleManager.GetString("Tool_InfoDragCenter", Culture);

		/// <summary>
		/// Drag to change rotate
		/// </summary>
		public static string Tool_InfoDragCircle => LocaleManager.GetString("Tool_InfoDragCircle", Culture);

		/// <summary>
		/// Drag to change corner offset
		/// </summary>
		public static string Tool_InfoDragCorner => LocaleManager.GetString("Tool_InfoDragCorner", Culture);

		/// <summary>
		/// Drag this end to change main slope direction
		/// </summary>
		public static string Tool_InfoDragMainSlopeDirectionEnd => LocaleManager.GetString("Tool_InfoDragMainSlopeDirectionEnd", Culture);

		/// <summary>
		/// Drop this end to change main slope direction
		/// </summary>
		public static string Tool_InfoDropMainSlopeDirectionEnd => LocaleManager.GetString("Tool_InfoDropMainSlopeDirectionEnd", Culture);

		/// <summary>
		/// Click to insert crossing node
		/// </summary>
		public static string Tool_InfoInsertCrossingNode => LocaleManager.GetString("Tool_InfoInsertCrossingNode", Culture);

		/// <summary>
		/// Click with {0} to insert crossing node
		/// </summary>
		public static string Tool_InfoInsertCrossingNodeWithModifier => LocaleManager.GetString("Tool_InfoInsertCrossingNodeWithModifier", Culture);

		/// <summary>
		/// Click to insert node
		/// </summary>
		public static string Tool_InfoInsertNode => LocaleManager.GetString("Tool_InfoInsertNode", Culture);

		/// <summary>
		/// Click with {0} to insert node
		/// </summary>
		public static string Tool_InfoInsertNodeWithModifier => LocaleManager.GetString("Tool_InfoInsertNodeWithModifier", Culture);

		/// <summary>
		/// Select corner you want to align with
		/// </summary>
		public static string Tool_InfoSelectAlignRelative => LocaleManager.GetString("Tool_InfoSelectAlignRelative", Culture);

		/// <summary>
		/// Change to desired main slope direction
		/// </summary>
		public static string Tool_InfoSelectMainSlopeDirection => LocaleManager.GetString("Tool_InfoSelectMainSlopeDirection", Culture);

		/// <summary>
		/// Select end position of main slope direction
		/// </summary>
		public static string Tool_InfoSelectNewMainSlopeDirectionEnd => LocaleManager.GetString("Tool_InfoSelectNewMainSlopeDirectionEnd", Culture);

		/// <summary>
		/// Select node
		/// </summary>
		public static string Tool_InfoSelectNode => LocaleManager.GetString("Tool_InfoSelectNode", Culture);

		/// <summary>
		/// Select corners you want to align
		/// </summary>
		public static string Tool_InfoSelectToAlign => LocaleManager.GetString("Tool_InfoSelectToAlign", Culture);

		/// <summary>
		/// Can't insert node
		/// </summary>
		public static string Tool_InfoTooCloseNode => LocaleManager.GetString("Tool_InfoTooCloseNode", Culture);

		/// <summary>
		/// Hold {0} to
		/// </summary>
		public static string Tool_InfoUnderground => LocaleManager.GetString("Tool_InfoUnderground", Culture);
	}
}