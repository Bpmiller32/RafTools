import { defineStore } from "pinia";
import { ref, watch } from "vue";

export const useStore = defineStore("main", () => {
  const currRoute = ref(null);
  const prevRoute = ref(null);

  const connection = ref(null);
  const crawlers = ref({
    SmartMatch: {
      autoCrawlStatus: null,
      autoCrawlEnabled: null,
      autoCrawlDate: null,
      directories: [],
    },
    Parascript: {
      autoCrawlStatus: null,
      autoCrawl: null,
      autoCrawlDate: null,
      directories: [],
    },
    RoyalMail: {
      autoCrawlStatus: null,
      autoCrawl: null,
      autoCrawlDate: null,
      directories: [],
    },
  });

  const SendMessageCrawler = (crawler, property, value) => {
    // Perform ws logic
    // this.connection ...

    // TODO: Placeholder ws response
    crawlers.value[crawler][property] = value;
  };
  const SendMessageForceCrawl = (crawler, value) => {
    // Perform ws logic
    // TODO: Placeholder
    crawlers.value[crawler].autoCrawlStatus = value;
  };

  // Persist the state of currRoute and prevRoute
  if (sessionStorage.getItem("routes")) {
    const routes = JSON.parse(sessionStorage.getItem("routes"));

    currRoute.value = routes[0];
    prevRoute.value = routes[1];
  }
  watch([currRoute, prevRoute], ([newCurrRoute, newPrevRoute]) => {
    sessionStorage.setItem(
      "routes",
      JSON.stringify([newCurrRoute, newPrevRoute])
    );
  });

  return {
    connection,
    currRoute,
    prevRoute,
    crawlers,
    SendMessageCrawler,
    SendMessageForceCrawl,
  };
});
