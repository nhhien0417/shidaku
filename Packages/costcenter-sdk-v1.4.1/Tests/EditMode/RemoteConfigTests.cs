using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CostCenter.RemoteConfig.Tests
{
    public class RemoteConfigTests
    {

        [Test]
        public void IsMapWithConversionData_Campaign_Case1()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "campaign_id", "12345" },
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Campaign_Case2()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign_new" },
                { "campaign_id", "12345" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Campaign_Case3()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "campaign_id", "123456" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Campaign_Case4()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Campaign_Case5()
        {
            var config = new CCConversionConfig
            {
                campaign = "",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Campaign_Case6()
        {
            var config = new CCConversionConfig
            {
                campaign_id = "",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adset_Case1()
        {
            var config = new CCConversionConfig
            {
                adset_id = "",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adset_Case2()
        {
            var config = new CCConversionConfig
            {
                adset = "test",
                adset_id = null,
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case1()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = "test_adgroup",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case2()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = "",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case3()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = null,
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case4()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = null,
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", "" },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case5()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = "test_adgroup",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", "test_adgroup_1" },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Adgroup_Case6()
        {
            var config = new CCConversionConfig
            {
                adgroup_id = "test_adgroup",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", "test_adgroup" },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Mix_Case1()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
                adgroup_id = "test_adgroup",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", "test_adgroup" },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsTrue(config.IsMapWithConversionData(conversionData));
        }

        [Test]
        public void IsMapWithConversionData_Mix_Case2()
        {
            var config = new CCConversionConfig
            {
                campaign = "test_campaign",
                campaign_id = "12345",
                adgroup_id = "test_adgroup",
            };

            var conversionData = new Dictionary<string, object>
            {
                { "campaign", "test_campaign" },
                { "campaign_id", "12345" },
                { "adset", "test_adset" },
                { "adset_id", null },
                { "adgroup_id", null },
                { "media_source", "test_media_source" },
                { "install_time", "2023-10-01T12:00:00Z" },
                { "af_siteid", "test_af_siteid" },
                { "extra_field", "extra_value" } // Extra field should not affect the result
            };

            Assert.IsFalse(config.IsMapWithConversionData(conversionData));
        }
    }
}
