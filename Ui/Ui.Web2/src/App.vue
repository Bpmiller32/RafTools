<script setup>
import { onMounted, ref, watch } from "vue";
import { useStore } from "./store";
import NavBar from "./components/NavBar.vue";
import ErrorPage from "./components/ErrorPage.vue";
import AnimationHandler from "./components/AnimationHandler.vue";
import LoadingPage from "./components/LoadingPage.vue";

const store = useStore();
const backendConnected = ref("loading");
const loadedData = ref({
  SmartMatchCrawler: false,
  ParascriptCrawler: false,
  RoyalMailCrawler: false,

  SmartMatchBuilder: false,
  ParascriptBuilder: false,
  RoyalMailBuilder: false,
});

const routes = ref(
  new Map([
    ["Home Home", "FadeIn"],
    ["Home Builder", "FromLeftToRight"],
    ["Home Tester", "FromLeftToRight"],

    ["Builder Builder", "FadeIn"],
    ["Builder Home", "FromRightToLeft"],
    ["Builder Tester", "FromLeftToRight"],

    ["Tester Tester", "FadeIn"],
    ["Tester Home", "FromRightToLeft"],
    ["Tester Builder", "FromRightToLeft"],
  ])
);

// OnMounted
onMounted(() => {
  // Create WebSocket connections inside of Pinia store
  store.connectionCrawler = new WebSocket("ws://192.168.0.39:10021");
  store.connectionBuilder = new WebSocket("ws://192.168.0.39:10022");

  // Connected to and disconnecting from backend services
  store.connectionCrawler.onopen = () => {
    console.log("Successfully connected to Crawler back end");
  };
  store.connectionCrawler.onclose = () => {
    console.log("Unable to connect to Crawler back end");
    backendConnected.value = "error";
  };
  store.connectionBuilder.onopen = () => {
    console.log("Successfully connected to Builder back end");
  };
  store.connectionBuilder.onclose = () => {
    console.log("Unable to connect to Builder back end");
    backendConnected.value = "error";
  };

  // OnMessage recieving data from backend services
  store.connectionCrawler.onmessage = (event) => {
    console.log("CrawlerData: ", JSON.parse(event.data), Date());
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.crawlers.SmartMatch = response.SmartMatch;
      loadedData.value.SmartMatchCrawler = true;
    }
    if (response.Parascript != null) {
      store.crawlers.Parascript = response.Parascript;
      loadedData.value.ParascriptCrawler = true;
    }
    if (response.RoyalMail != null) {
      store.crawlers.RoyalMail = response.RoyalMail;
      loadedData.value.RoyalMailCrawler = true;
    }
  };
  store.connectionBuilder.onmessage = (event) => {
    console.log("BuilderData: ", JSON.parse(event.data), Date());
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.builders.SmartMatch = response.SmartMatch;
      loadedData.value.SmartMatchBuilder = true;
    }
    if (response.Parascript != null) {
      store.builders.Parascript = response.Parascript;
      loadedData.value.ParascriptBuilder = true;
    }
    if (response.RoyalMail != null) {
      store.builders.RoyalMail = response.RoyalMail;
      loadedData.value.RoyalMailBuilder = true;
    }
  };
});

// Watchers
watch(
  () => loadedData.value,
  () => {
    if (
      loadedData.value.SmartMatchCrawler &&
      loadedData.value.ParascriptCrawler &&
      loadedData.value.RoyalMailCrawler &&
      loadedData.value.ParascriptBuilder &&
      loadedData.value.RoyalMailBuilder
    ) {
      backendConnected.value = "connected";
    }
  },
  { deep: true }
);
</script>

<template>
  <div v-if="backendConnected == 'connected'">
    <NavBar class="fixed top-0 left-0 right-0 z-50" />
    <router-view
      class="absolute top-16 left-0 right-0"
      v-slot="{ Component, route }"
    >
      <AnimationHandler
        :animation="routes.get(`${route.meta.fromRoute} ${route.meta.toRoute}`)"
        transitionMode="default"
      >
        <component :is="Component" />
      </AnimationHandler>
    </router-view>
  </div>
  <div v-else-if="backendConnected == 'loading'">
    <LoadingPage />
  </div>
  <div v-else>
    <ErrorPage />
  </div>
</template>
