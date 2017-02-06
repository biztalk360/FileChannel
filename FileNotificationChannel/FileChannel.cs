using B360.Notifier.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace FileNotificationChannel
{
    // Note: "System.Runtime.Serialization" -- This Assembly need to be added as a reference to this Project.
    public class FileChannel : IChannelNotification
    {
        public string GetGlobalPropertiesSchema()
        {
            return Helper.GetResourceFileContent("GlobalProperties.xml");
        }

        public string GetAlarmPropertiesSchema()
        {
            return Helper.GetResourceFileContent("AlarmProperties.xml");
        }

        public bool SendNotification(BizTalkEnvironment environment, Alarm alarm, string globalProperties, Dictionary<MonitorGroupTypeName, MonitorGroupData> notifications)
        {
            try
            {
                //Construct Message
                var message = string.Empty;
                message += string.Format("\nAlarm Name: {0} \n\nAlarm Desc: {1} \n", alarm.Name, alarm.Description);
                message += "\n----------------------------------------------------------------------------------------------------\n";
                message += string.Format("\nEnvironment Name: {0} \n\nMgmt Sql Instance Name: {1} \nMgmt Sql Db Name: {2}\n", environment.Name, environment.MgmtSqlDbName, environment.MgmtSqlInstanceName);
                message += "\n----------------------------------------------------------------------------------------------------\n";

                // Change the Message Type and Format based on your need.
                var helper = new BT360Helper(notifications, environment, alarm, MessageType.ConsolidatedMessage, MessageFormat.Text);
                message += helper.GetNotificationMessage();

                //Read configured properties
                var bsd = XNamespace.Get("http://www.biztalk360.com/alarms/notification/basetypes");

                var fileFolder = string.Empty; var folderOverride = string.Empty;

                //Alarm Properties
                var almProps = XDocument.Load(new StringReader(alarm.AlarmProperties));
                foreach (var element in almProps.Descendants(bsd + "TextBox"))
                {
                    var xAttribute = element.Attribute("Name");
                    if (xAttribute == null || xAttribute.Value != "file-override-path") continue;
                    var attribute = element.Attribute("Value");
                    if (attribute != null) folderOverride = attribute.Value;
                }

                //Global Properties
                var globalProps = XDocument.Load(new StringReader(globalProperties));
                foreach (var element in globalProps.Descendants(bsd + "TextBox"))
                {
                    var xAttribute = element.Attribute("Name");
                    if (xAttribute == null || xAttribute.Value != "file-path") continue;
                    var attribute = element.Attribute("Value");
                    if (attribute != null) fileFolder = attribute.Value;
                }

                //Save to Disk
                var filePath = string.IsNullOrEmpty(folderOverride) ? fileFolder : folderOverride;
                var fileLocation = Path.Combine(filePath, Guid.NewGuid() + ".txt");
                using (var fs = new FileStream(fileLocation, FileMode.CreateNew))
                {
                    fs.Write(Encoding.UTF8.GetBytes(message), 0, message.Length);
                }
                LoggingHelper.Info("File notification completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                LoggingHelper.Info("File notification failed. Error " + ex.Message);
                return false;
                // ignored
            }

        }
    }
}
