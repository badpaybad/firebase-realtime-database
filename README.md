# firebase-realtime-database

app.config

get you root firebase realtime db url
https://drive.google.com/file/d/1wbAEAH9XU_zhcmxGmPVh2Neygc7Jllxm/view?usp=sharing

get you google credential file (will place in root folder)
https://drive.google.com/file/d/1ZCrR_xuTxPHHvVO0Vfm5I2gmwLxrK2Vq/view?usp=sharing

# usage
        var helper = new RealtimeDatabaseHelper()
        helper.Publish("ref_url", new{YourData="Hello world"});
        
        helper.Subscribe("ref_url", (evt, data, refurl, fullurl)=>{ Console.WriteLine("Do with your data"); })
