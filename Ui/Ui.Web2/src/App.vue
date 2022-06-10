<script setup>
import { onMounted, ref } from "vue";
import { useStore } from "./store";
import NavBar from "./components/NavBar.vue";
import ErrorPage from "./components/ErrorPage.vue";
import AnimationHandler from "./components/AnimationHandler.vue";
import LoadingPage from "./components/LoadingPage.vue";

const store = useStore();
const backendConnected = ref("loading");
const loadedData = ref({
  SmartMatch: false,
  Parascript: false,
  RoyalMail: false,
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

onMounted(() => {
  store.connectionCrawler = new WebSocket("ws://192.168.50.184:10021");
  store.connectionBuilder = new WebSocket("ws://192.168.50.184:10022");

  store.connectionCrawler.onopen = () => {
    console.log("Successfully connected to back end");
  };
  store.connectionCrawler.onclose = () => {
    console.log("Unable to connect to back end");
    backendConnected.value = "error";
  };
  store.connectionBuilder.onopen = () => {
    console.log("Successfully connected to back end");
  };
  store.connectionBuilder.onclose = () => {
    console.log("Unable to connect to back end");
    backendConnected.value = "error";
  };

  store.connectionCrawler.onmessage = (event) => {
    console.log("CrawlerData: ", JSON.parse(event.data), Date());
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.crawlers.SmartMatch = response.SmartMatch;
      loadedData.value.SmartMatch = true;
    }
    if (response.Parascript != null) {
      store.crawlers.Parascript = response.Parascript;
      loadedData.value.Parascript = true;
    }
    if (response.RoyalMail != null) {
      store.crawlers.RoyalMail = response.RoyalMail;
      loadedData.value.RoyalMail = true;
    }

    if (
      loadedData.value.SmartMatch == true &&
      loadedData.value.Parascript == true &&
      loadedData.value.RoyalMail == true
    ) {
      backendConnected.value = "connected";
    }
  };
  store.connectionBuilder.onmessage = (event) => {
    console.log("BuilderData: ", JSON.parse(event.data), Date());
  };
});
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
