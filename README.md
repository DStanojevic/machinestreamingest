# Overview

Demo app which have purpose to listening data from the web socket and persist it.
Also it expose REST Api for browsing data.

# Running the app

## Setting up image and container

### Building an image  

Navigate your console location to the repository root folder and execute command below:  
`docker build -t machine-data-ingest:1.0 -f Dockerfile .`

### Running the docker container  

Execute command below to have docker container up and running:  
`docker run -d -p 8080:80 --name machinedataapi machine-data-ingest:1.0`

# Exploring the app  

 ## OpenAPI specification
 
 Swagger endpoint is available at 'http://localhost:8080/swagger/index.html'.

 ## Browsing data  

 ### List all machines that emitted data

 ```cUrl
 curl --location --request GET 'http://localhost:8080/api/Machines/'
 ```

 Response example: 
 ```json
 [
    "db9eb448-214b-481f-96fe-d1b883ec11a7",
    "63f9d31e-18cf-4def-8887-164366d70c46",
    "5b3ec85c-d2ff-404f-aa6b-0a5e82537caa",
    "cad031e6-e4ed-4d9a-b10d-ff9920d32b4e",
    "d00b151e-d488-4ffd-b1fc-a84587e9fb28",
    "68015cc1-3119-42d2-9d4e-3e824723fe03",
    "45958000-f702-41be-93a8-9a7e3edd4121",
    "95d9b02f-3347-4c0c-a8a0-6e6e525121d5",
    "d29675bc-f3a4-424f-a9a1-68eb257bf30f",
    "265a2ba3-4609-4974-ba07-e5eed81839ea",
    "840c6335-c0b9-49f8-9eba-e52ef9e23c43",
    "799819f8-6c19-47cc-9e3f-b9438b3bed4f",
    "e0776fcc-b8e7-4927-943b-10235df7678c",
    "00eee2c7-ef69-4df9-94f9-c504ba2ce8a4"
]
 ```

 ### List measurements for particular machine  

 ```cUrl
 curl --location --request GET 'http://localhost:8080/api/Machines/d00b151e-d488-4ffd-b1fc-a84587e9fb28?skip=0&take=20'
 ```

 Response example:  
 ```json
 {
    "items": [
        {
            "id": "5be10d99-0339-4461-bb6b-ed78d515dd56",
            "machineId": "d00b151e-d488-4ffd-b1fc-a84587e9fb28",
            "timeStamp": "2022-07-08T08:43:46Z",
            "status": "Finished"
        },
        {
            "id": "69b5eaf0-df1f-49e2-80a3-8cc432e1118d",
            "machineId": "d00b151e-d488-4ffd-b1fc-a84587e9fb28",
            "timeStamp": "2022-07-08T08:47:02Z",
            "status": "Running"
        }
    ],
    "totalCount": 2
}
 ```

 ### List all messages  

 ```cUrl
 curl --location --request GET 'http://localhost:8080/api/Messages?take=3&skip=0'
 ```

 Response example:
 ```json
 {
    "items": [
        {
            "id": "9c6b45ff-faf3-4d4c-b9ee-04a70359c50b",
            "machineId": "68015cc1-3119-42d2-9d4e-3e824723fe03",
            "timeStamp": "2022-07-08T08:43:51Z",
            "status": "Running"
        },
        {
            "id": "a99644de-24e3-4e61-8957-790bcd4fbf0a",
            "machineId": "265a2ba3-4609-4974-ba07-e5eed81839ea",
            "timeStamp": "2022-07-08T08:45:46Z",
            "status": "Running"
        },
        {
            "id": "ac4a7ff4-b874-4bb2-9b9f-5bfafba6e1c8",
            "machineId": "db9eb448-214b-481f-96fe-d1b883ec11a7",
            "timeStamp": "2022-07-08T08:47:27Z",
            "status": "Running"
        }
    ],
    "totalCount": 40
}
 ```

 ### Retrieve particular message  

 ```cUrl
 curl --location --request GET 'http://localhost:8080/api/Messages/64ae54f6-baaf-4152-a117-c6d3a1302f2d'
 ```

 Response example:  
 ```json
 {
    "id": "64ae54f6-baaf-4152-a117-c6d3a1302f2d",
    "machineId": "759c98a4-b66f-4799-b278-bab8aec881a0",
    "timeStamp": "2022-07-08T08:47:02Z",
    "status": "Finished"
}
 ```