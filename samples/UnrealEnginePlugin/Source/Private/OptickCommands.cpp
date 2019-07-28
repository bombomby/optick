#include "OptickCommands.h"

#define LOCTEXT_NAMESPACE "FOptickModule"

void FOptickCommands::RegisterCommands()
{
	UI_COMMAND(PluginAction, "Optick", "Open Optick Profiler", EUserInterfaceActionType::Button, FInputGesture());
}

#undef LOCTEXT_NAMESPACE