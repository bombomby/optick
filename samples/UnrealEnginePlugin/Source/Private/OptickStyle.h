// Copyright(c) 2019 Vadim Slyusarev

#pragma once

#if WITH_EDITOR

#include "SlateCore/Public/Styling/SlateStyle.h"

/**  */
class FOptickStyle
{
public:

	static void Initialize();

	static void Shutdown();

	/** reloads textures used by slate renderer */
	static void ReloadTextures();

	/** @return The Slate style set for the Shooter game */
	static const ISlateStyle& Get();

	static FName GetStyleSetName();

private:

	static TSharedRef< class FSlateStyleSet > Create();

private:

	static TSharedPtr< class FSlateStyleSet > StyleInstance;
};

#endif