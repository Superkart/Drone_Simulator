// Copyright Epic Games, Inc. All Rights Reserved.
#pragma once

#include "CoreMinimal.h"
#include "WorldPartition/WorldPartitionVolume.h"
#include "CitySampleWorldPartitionVolume.generated.h"

UCLASS(hidecategories = (Cooking, LOD, HLOD, Input, WorldPartition, Collision, Replication, Rendering))
class ACitySampleWorldPartitionVolume : public AWorldPartitionVolume
{
	GENERATED_BODY()

public:
	ACitySampleWorldPartitionVolume(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get());

#if WITH_EDITOR
	UFUNCTION(BlueprintCallable, Category = CitySampleWorldPartition, meta = (CallInEditor = "true"))
	void CallLoadIntersectingCells();

	UFUNCTION(BlueprintCallable, Category = CitySampleWorldPartition, meta = (CallInEditor = "true"))
	void CallUnloadIntersectingCells();

	virtual void Tick(float DeltaTime) override;
	virtual void PostRegisterAllComponents() override;
	virtual bool ShouldTickIfViewportsOnly() const override { return true; }
private:
	virtual bool SupportsDataLayer() const override { return true; }

	bool bLoadIntersectingCellsWasCalled;
#endif

#if WITH_EDITORONLY_DATA
	UPROPERTY(EditAnywhere, Category = CitySampleWorldPartition)
	bool bLoadIntersectingCellsOnRegister;
#endif
};