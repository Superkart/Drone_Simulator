// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "vehicles/multirotor/api/MultirotorRpcLibClient.hpp"
#include "DroneApi.generated.h"


using namespace msr;


UCLASS()
class CITYSAMPLE_API ADroneApi : public AActor
{
	GENERATED_BODY()

public:
	// Variables
	airlib::MultirotorRpcLibClient client;
	bool isArmed = false;

	// Constructor
	ADroneApi();

	// Wrapper Functions
	UFUNCTION(BlueprintCallable, Category = "AirSimApi")
		void ConnectToDroneNetwork();

	UFUNCTION(BlueprintCallable, Category = "AirSimApi")
		void ArmDrone();

	UFUNCTION(BlueprintCallable, Category = "AirSimApi")
		float GetAltitudeDrone();

	UFUNCTION(BlueprintCallable, Category = "AirSimApi")
		float GetVelocityDrone();



	//Conversion Methods
	FVector Vector3toFVector(airlib::Vector3r vector);

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:
	// Called every frame
	virtual void Tick(float DeltaTime) override;

};
