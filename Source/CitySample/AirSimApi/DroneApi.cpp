// Fill out your copyright notice in the Description page of Project Settings.


#include "AirSimApi/DroneApi.h"

// Sets default values
ADroneApi::ADroneApi()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

}

void ADroneApi::ConnectToDroneNetwork()
{
	try {
		client.enableApiControl(true);
		client.confirmConnection();
	}
	catch (...) {
		GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow, TEXT("Your Message"));
	}

}

void ADroneApi::ArmDrone()
{
	client.armDisarm(true);
	isArmed = true;
}
float ADroneApi::GetAltitudeDrone()
{
	float altitude = (1200 - 1077.311) - client.getGpsData().gnss.geo_point.altitude;
	return altitude;
}
float ADroneApi::GetVelocityDrone()
{
	airlib::GpsBase::GnssReport gnss = client.getGpsData().gnss;
	FVector velocity_U = Vector3toFVector(gnss.velocity);
	float x = velocity_U.Size();
	return x;
}

//Conversion methods
FVector ADroneApi::Vector3toFVector(airlib::Vector3r vector)
{
	FVector temp;
	temp.X = vector.x();
	temp.Y = vector.y();
	temp.Z = vector.z();
	return temp;
}



// Called when the game starts or when spawned
void ADroneApi::BeginPlay()
{
	Super::BeginPlay();
}

// Called every frame
void ADroneApi::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	//ConnectToDroneNetwork();

}

