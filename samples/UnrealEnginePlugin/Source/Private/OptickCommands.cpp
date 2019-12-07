// Copyright(c) 2019 Vadim Slyusarev

#include "OptickCommands.h"

#if WITH_EDITOR

#define LOCTEXT_NAMESPACE "FOptickModule"

void FOptickCommands::RegisterCommands()
{
	UI_COMMAND(PluginAction, "Optick", "Open Optick Profiler", EUserInterfaceActionType::Button, FInputGesture());
}

#undef LOCTEXT_NAMESPACE

#endif