import { defineStore } from "pinia";
import { ref } from "vue";

export const useStore = defineStore("main", () => {
  const connection = ref(null);
  const crawlers = ref({
    SmartMatch: {
      AutoCrawlStatus: null,
      AutoCrawlEnabled: null,
      AutoCrawlDate: null,
      AvailableBuilds: [],
    },
    Parascript: {
      AutoCrawlStatus: null,
      AutoCrawl: null,
      AutoCrawlDate: null,
      AvailableBuilds: [],
    },
    RoyalMail: {
      AutoCrawlStatus: null,
      AutoCrawl: null,
      AutoCrawlDate: null,
      AvailableBuilds: [],
    },
  });

  function SendMessageUpdate(crawler, property, value) {
    connection.value.send(JSON.stringify({ crawler, property, value }));
  }
  function SendMessageForce(crawler) {
    if (crawler == "SmartMatch") {
      connection.value.send(
        JSON.stringify({ crawler: crawler, property: "Force", value: "Force" })
      );
    } else if (crawler == "Parascript") {
      connection.value.send(
        JSON.stringify({ crawler: crawler, property: "Force", value: "Force" })
      );
    } else if (crawler == "RoyalMail") {
      connection.value.send(
        JSON.stringify({ crawler: crawler, property: "Force", value: "Force" })
      );
    }
  }

  return {
    connection,
    crawlers,
    SendMessageUpdate,
    SendMessageForce,
  };
});
