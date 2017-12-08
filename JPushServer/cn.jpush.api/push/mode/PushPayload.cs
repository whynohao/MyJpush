﻿using cn.jpush.api.common;
using cn.jpush.api.util;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace cn.jpush.api.push.mode
{
    public class PushPayload
    {
        private JsonSerializerSettings jSetting;

        private const string PLATFORM = "platform";
        private const string AUDIENCE = "audience";
        private const string NOTIFICATION = "notification";
        private const string MESSAGE = "message";
        private const string SMS_MESSAGE = "sms_message";
        private const string OPTIONS = "options";

        private const int MAX_IOS_ENTITY_LENGTH = 6144;  // Definition according to JPush Docs
        private const int MAX_IOS_PAYLOAD_LENGTH = 2048;  // Definition according to JPush Docs
        private const int MAX_ANDROID_ENTITY_LENGTH = 4096;

        [JsonConverter(typeof(PlatformConverter))]
        public Platform platform { get; set; }

        [JsonConverter(typeof(AudienceConverter))]
        public Audience audience { get; set; }

        public Notification notification { get; set; }
        public Message message { get; set; }
        public SmsMessage sms_message { get; set; }
        public Options options { get; set; }

        public PushPayload()
        {
            platform = null;
            audience = null;
            notification = null;
            message = null;
            sms_message = null;
            options = new Options();
            jSetting = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        public PushPayload(Platform platform, Audience audience, Notification notification,
            Message message = null, SmsMessage sms_message = null, Options options = null)
        {
            Debug.Assert(platform != null);
            Debug.Assert(audience != null);
            Debug.Assert(notification != null || message != null);

            this.platform = platform;
            this.audience = audience;
            this.notification = notification;
            this.message = message;
            this.sms_message = sms_message;
            this.options = options;

            jSetting = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
        }

        // The shortcut of building a simple alert notification object to all platforms and all audiences.
        public static PushPayload AlertAll(string alert)
        {
            return new PushPayload(Platform.all(),
                                   Audience.all(),
                                   new Notification().setAlert(alert),
                                   null,
                                   null,
                                   new Options());
        }

        // The shortcut of building a simple message object to all platforms and all audiences.
        public static PushPayload MessageAll(string msgContent)
        {
            return new PushPayload(Platform.all(),
                                   Audience.all(),
                                   null,
                                   Message.content(msgContent),
                                   null,
                                   new Options());
        }

        /// <summary>
        /// It need to have a notification or message.
        /// </summary>
        /// <param name="smsContent"></param>
        /// <returns></returns>
        public static PushPayload FromJSON(string payloadString)
        {
            try
            {
                var jSetting = new JsonSerializerSettings();
                jSetting.NullValueHandling = NullValueHandling.Ignore;
                jSetting.DefaultValueHandling = DefaultValueHandling.Ignore;

                var jsonObject = JsonConvert.DeserializeObject<PushPayload>(payloadString, jSetting);
                return jsonObject.Check();
            }
            catch (Exception e)
            {
                Console.WriteLine("JSON to PushPayLoad occur error:" + e.Message);
                return null;
            }
        }

        public void ResetOptionsApnsProduction(bool apnsProduction)
        {
            if (options == null)
            {
                options = new Options();
            }
            options.apns_production = apnsProduction;
        }

        public void ResetOptionsTimeToLive(long timeToLive)
        {
            if (options == null)
            {
                options = new Options();
            }
            options.time_to_live = timeToLive;
        }

        public int GetSendno()
        {
            if (options != null)
                return options.sendno;
            return 0;
        }

        public bool IsGlobalExceedLength()
        {
            return IsAndroidExceedLength() && IsiOSExceedLength();
        }

        public bool IsiOSExceedLength()
        {
            int messageLength = 0;
            if (message != null)
            {
                var messageJson = JsonConvert.SerializeObject(message, jSetting);
                messageLength += Encoding.UTF8.GetBytes(messageJson).Length;
            }

            if (notification == null)
            {
                return messageLength > MAX_IOS_ENTITY_LENGTH;
            }
            else
            {
                var notificationJson = JsonConvert.SerializeObject(notification);
                if (notificationJson != null)
                {
                    int iosJsonLength = 0;
                    if (notification.IosNotification != null)
                    {
                        var iosJson = JsonConvert.SerializeObject(notification.IosNotification, jSetting);
                        if (iosJson != null)
                        {
                            iosJsonLength = Encoding.UTF8.GetBytes(iosJson).Length;
                        }
                    }
                    messageLength += Encoding.UTF8.GetBytes(notificationJson).Length;
                    messageLength -= iosJsonLength;
                }
                return messageLength > MAX_IOS_ENTITY_LENGTH;
            }
        }

        public bool IsAndroidExceedLength()
        {
            int messageLength = 0;
            if (message != null)
            {
                var messageJson = JsonConvert.SerializeObject(message, jSetting);
                messageLength += Encoding.UTF8.GetBytes(messageJson).Length;
            }

            if (notification == null)
            {
                return messageLength > MAX_ANDROID_ENTITY_LENGTH;
            }
            else
            {
                var notificationJson = JsonConvert.SerializeObject(notification.AndroidNotification);
                if (notificationJson != null)
                {
                    int androidJsonLength = 0;
                    if (notification.AndroidNotification != null)
                    {
                        var androidJson = JsonConvert.SerializeObject(notification.AndroidNotification, jSetting);
                        if (androidJson != null)
                        {
                            androidJsonLength = UTF8Encoding.UTF8.GetBytes(androidJson).Length;
                        }
                    }
                    messageLength += UTF8Encoding.UTF8.GetBytes(notificationJson).Length;
                    messageLength -= androidJsonLength;
                }
                return messageLength > MAX_ANDROID_ENTITY_LENGTH;
            }
        }

        public bool IsIosExceedLength()
        {
            if (notification != null)
            {
                if (notification.IosNotification != null)
                {
                    var iosJson = JsonConvert.SerializeObject(notification.IosNotification, jSetting);
                    if (iosJson != null)
                    {
                        return Encoding.UTF8.GetBytes(iosJson).Length > MAX_IOS_PAYLOAD_LENGTH;
                    }
                }
                else
                {
                    if (!(notification.alert == null))
                    {
                        string jsonText;
                        using (StringWriter sw = new StringWriter())
                        {
                            JsonWriter writer = new JsonTextWriter(sw);
                            writer.WriteValue(notification.alert);
                            writer.Flush();
                            jsonText = sw.GetStringBuilder().ToString();
                        }
                        return Encoding.UTF8.GetBytes(jsonText).Length > MAX_IOS_PAYLOAD_LENGTH;
                    }
                }

            }
            return false;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, jSetting);
        }

        public PushPayload Check()
        {
            Preconditions.checkArgument(!(null == audience || null == platform), "audience and platform both should be set.");
            Preconditions.checkArgument(!(null == notification && null == message), "notification or message should be set at least one.");

            if (audience != null)
            {
                audience.Check();
            }
            if (platform != null)
            {
                platform.Check();
            }
            if (message != null)
            {
                message.Check();
            }
            if (notification != null)
            {
                notification.Check();
            }
            return this;
        }
    }
}
