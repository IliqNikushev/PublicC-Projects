# PublicC-Projects
Repositiory containing my public c# projects

1. Transmission agent
It is a DLL that provides acces for an Application - Application connection

I am using it in order to make communication between WCF ( Windows Communication Foundation) and Unity3D
By having a Service, Client and Game running, The Game sends commands to the Client that passes them to the Service
Then the Service responds to the client that passes it to the Game client

The final version will be published once it is complete

How to use:
1. Create an Application that is a 'Receiver'
Create a TransmissionAgent.Receiver and attach it to a port
Attach to the OnMessageReceived event your event handler

2. Create an Application that is a 'Client'
Create a TransmissionAgent.Sender and attach it to the port and connect to the Local ip address ( Utils.LocalIpAddress)
Attach to the OnMessageReceived event your event handler
Alternativelly, it can be attached to another computer's IP, if both machines are on a local network

Example :
	Receiver
		OnMessageReceived += (x) => 
		{
			if(x is TransmissionAgent.InvokeMethodMessage)
				this.Service.Invoke((x as TransmissionAgent.InvokeMethodMessage).Data.MethodName)
		}
	Client
		OnMessageReceived += (x) =>
		{
			if(x is TransmissionAgent.InvokeMethodResultMessage)
				Console.WriteLine((x as InvokeMethodResultMessage).Data.Result)
		}

2.Thread Control
A test i made in 2013 to see how Threading works, by having ThreadPool implemented

Basic idea of a thread pool is:
Thread does a job if there is any, otherwise it is in a 'wait' state
Once a job is issued, a 'waiting' thread is given that task and taken out of the 'waiting' state
if no thread is free, save the task for later
Once a thread is finished, it requests a new task to complete

This is an average output on my machine:
24 Threads @ ThreadControl.Instance
1023 Threads @ System.Threading.ThreadPool
10000 completed by ThreadControl
10000 completed by ThreadPool
00:00:00.9686085 ThreadControl's time
00:00:00.9995839 ThreadPool's time

ThreadControl varies between 0.95 and 0.99
ThreadPool varies between 0.95 and 1.33
Sometimes ThreadPool completes with times > 2.0

When the application is run

3.ImageComparerSimple
This is an application i made in 2013 that looks for duplicate images across folders

How to use:
1. Provide folders with the original images seperated by ; ( or leave blank, will use folders.txt)
2. Provide folders where duplicates might be found ( or leave blank, will use folders.txt)
3. Provide id of last image searched if any
4. Provide id of last comaprison image searched if any

The application will go through all images in the original folders and compare them to each image in the duplicate folders

The application is multithreaded up to 24 Threads. This prohibits memory overflow due to opening large images. Multithreading also works only for all files in the current duplicate folder, again to preserve memory

