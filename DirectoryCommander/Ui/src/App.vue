<script setup>
import { onMounted, ref } from "vue";
import { useStore } from "./store";
import NavBar from "./components/NavBar.vue";
import AnimationHandler from "./components/AnimationHandler.vue";

const store = useStore();

const routes = ref(
  new Map([
    ["Home Home", "FadeIn"],
    ["Home Builder", "FromLeftToRight"],
    ["Home Tester", "FromLeftToRight"],
    ["Home Publish", "FromLeftToRight"],

    ["Builder Home", "FromRightToLeft"],
    ["Builder Builder", "FadeIn"],
    ["Builder Tester", "FromLeftToRight"],
    ["Builder Publish", "FromLeftToRight"],

    ["Tester Home", "FromRightToLeft"],
    ["Tester Builder", "FromRightToLeft"],
    ["Tester Tester", "FadeIn"],
    ["Tester Publish", "FromLeftToRight"],

    ["Publish Home", "FromRightToLeft"],
    ["Publish Builder", "FromRightToLeft"],
    ["Publish Tester", "FromRightToLeft"],
    ["Publish Publish", "FadeIn"],
  ])
);

// OnMounted
onMounted(() => {
  // Create WebSocket connections inside of Pinia store
  store.connectionCrawler = new WebSocket("ws://192.168.0.39:10021");
  store.connectionBuilder = new WebSocket("ws://192.168.0.39:10022");
  store.connectionTester = new WebSocket("ws://192.168.0.39:10023");

  // Connected to and disconnecting from backend services
  store.connectionCrawler.onopen = () => {
    console.log("Successfully connected to Crawler back end");
  };
  store.connectionBuilder.onopen = () => {
    console.log("Successfully connected to Builder back end");
  };
  store.connectionTester.onopen = () => {
    console.log("Successfully connected to Tester back end");
  };

  // OnMessage recieving data from backend services
  store.connectionCrawler.onmessage = (event) => {
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.crawlers.SmartMatch = response.SmartMatch;
      store.crawlers.SmartMatch.DataRecieved = true;
    }
    if (response.Parascript != null) {
      store.crawlers.Parascript = response.Parascript;
      store.crawlers.Parascript.DataRecieved = true;
    }
    if (response.RoyalMail != null) {
      store.crawlers.RoyalMail = response.RoyalMail;
      store.crawlers.RoyalMail.DataRecieved = true;
    }
  };
  store.connectionBuilder.onmessage = (event) => {
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.builders.SmartMatch = response.SmartMatch;
    }
    if (response.Parascript != null) {
      store.builders.Parascript = response.Parascript;
    }
    if (response.RoyalMail != null) {
      store.builders.RoyalMail = response.RoyalMail;
    }
  };
  store.connectionTester.onmessage = (event) => {
    const response = JSON.parse(event.data);

    if (response.SmartMatch != null) {
      store.testers.SmartMatch = response.SmartMatch;
      store.testers.SmartMatch.DataRecieved = true;
    }
    if (response.Parascript != null) {
      store.testers.Parascript = response.Parascript;
      store.testers.Parascript.DataRecieved = true;
    }
    if (response.RoyalMail != null) {
      store.testers.RoyalMail = response.RoyalMail;
      store.testers.RoyalMail.DataRecieved = true;
    }
    if (response.Zip4 != null) {
      store.testers.Zip4 = response.Zip4;
      store.testers.Zip4.DataRecieved = true;
    }
  };
});
</script>

<template>
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
</template>
