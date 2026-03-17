// Copyright Epic Games, Inc. All Rights Reserved.

#include "WorldPartition/CitySampleWorldPartitionVolume.h"
#include "Engine/World.h"

ACitySampleWorldPartitionVolume::ACitySampleWorldPartitionVolume(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
#if WITH_EDITORONLY_DATA
	, bLoadIntersectingCellsWasCalled(false)
	, bLoadIntersectingCellsOnRegister(true)
#endif
{
#if WITH_EDITOR
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.bStartWithTickEnabled = false;
#endif
}

#if WITH_EDITOR
void ACitySampleWorldPartitionVolume::PostRegisterAllComponents()
{
	Super::PostRegisterAllComponents();

	if (!bIsEditorPreviewActor)
	{
		// bLoadIntersectingCellsWasCalled exists to make sure if volume gets unregistered/reregistered we don't reenable the tick again
		if (!bLoadIntersectingCellsWasCalled && bLoadIntersectingCellsOnRegister && !GetWorld()->IsGameWorld())
		{
			SetActorTickEnabled(true);
		}
	}
}

void ACitySampleWorldPartitionVolume::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	check(!bLoadIntersectingCellsWasCalled);
	SetActorTickEnabled(false);
	LoadIntersectingCells(false);
	bLoadIntersectingCellsWasCalled = true;
}

void ACitySampleWorldPartitionVolume::CallLoadIntersectingCells()
{
	LoadIntersectingCells(false);
}

void ACitySampleWorldPartitionVolume::CallUnloadIntersectingCells()
{
	UnloadIntersectingCells(false);
}
#endif

