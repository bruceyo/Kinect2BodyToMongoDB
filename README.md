Kinect2BodyToMongoDB Version 1.0 Brief Description

What it does:
Retrieve data from MS Kinect v2 sensor and store the time series body stream data to MongoDB 3.0
Time series body data inserted to MongoDB:
  1. Joints
  2. Jointorientations
  3. Hands
  3. Lean

Develop Environment:
Visual Studio 2013 Ultimate on Win10, MongoDB 3.0 on Ubuntu 16.04 LTS

C# MongoDB Driver:
  mongo-C#-Driver-2.3.0 <https://mongodb.github.io/mongo-csharp-driver/>

Limitations:
  1. Without audio data. 
  2. If want to keep the raw data, it needs to record the raw data then play the raw data for inserting into MongoDB.
