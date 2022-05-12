<script setup>
// This starter template is using Vue 3 <script setup> SFCs
// Check out https://vuejs.org/api/sfc-script-setup.html#script-setup
import { onMounted, ref } from "vue";
import { useStore } from "./store";
import { useRouter } from "vue-router";
import NavBar from "./components/NavBar.vue";
import ErrorPage from "./components/ErrorPage.vue";
import AnimationHandler from "./components/AnimationHandler.vue";
import CrawlerPageVue from "./components/CrawlerPage.vue";
import HelloWorldVue from "./components/HelloWorld.vue";

// ****
// TODO: Take out routing, simply swap components in component tag
// ****

const store = useStore();
const backendConnected = ref(true);
const router = useRouter();

const routerState = ref({
  currentComponent: CrawlerPageVue,
  components: [CrawlerPageVue, HelloWorldVue],
  routeAnimation: null,
  routes: new Map([
    ["Home Home", "FadeInDown"],
    ["Home Builder", "FadeInDown"],
    ["Home Tester", "FadeInDown"],
    ["Builder Builder", "FadeInDown"],
    ["Builder Home", "FadeInDown"],
    ["Builder Tester", "FadeInDown"],
    ["Tester Tester", "FadeInDown"],
    ["Tester Home", "FadeInDown"],
    ["Tester Builder", "FadeInDown"],
  ]),
  SetAnimation: () => {
    console.log("mounted in app prevRoute: ", store.prevRoute);
    console.log("mounted in app currRoute: ", store.currRoute);

    routerState.value.routeAnimation = routerState.value.routes.get(
      `${store.prevRoute} ${store.currRoute}`
    );

    console.log(routerState.value.routes.get(routerState.value.routeAnimation));
  },
});

onMounted(async () => {
  await router.isReady();
  // routerState.value.SetAnimation();

  // if (sessionStorage.getItem("Store-Key")) {
  //   console.log("Store-Key exists already");
  // }

  // Tests
  // store.crawlers.SmartMatch.directories.push({
  //   name: "202205",
  //   fileCount: 6,
  //   downloadDate: "05/01/22",
  // });
  // store.crawlers.SmartMatch.directories.push({
  //   name: "202204",
  //   fileCount: 6,
  //   downloadDate: "04/15/22",
  // });
  store.crawlers.SmartMatch.autoCrawlStatus = "Ready";
  store.crawlers.SmartMatch.autoCrawlEnabled = true;
  store.crawlers.SmartMatch.autoCrawlDate = "10/02/1991";
  store.crawlers.Parascript.autoCrawlStatus = "Error";
  store.crawlers.Parascript.autoCrawlEnabled = true;
  store.crawlers.Parascript.autoCrawlDate = "10/02/1991";
  store.crawlers.RoyalMail.autoCrawlStatus = "In Progress";
  store.crawlers.RoyalMail.autoCrawlEnabled = true;
  store.crawlers.RoyalMail.autoCrawlDate = "10/02/1991";
  setTimeout(() => {
    store.crawlers.SmartMatch.autoCrawlStatus = "In Progress";
    store.crawlers.SmartMatch.autoCrawlEnabled = false;
    store.crawlers.SmartMatch.autoCrawlDate = "11/11/1111";
    store.crawlers.SmartMatch.directories.push({
      name: "202203",
      fileCount: 6,
      downloadDate: "03/12/22",
    });
  }, 15000);

  // Websocket
  // console.log("Starting connection to websocket server");
  // store.connection = new WebSocket("ws://192.168.50.184:10022");
  // store.connection.onmessage = function (event) {
  //   console.log(JSON.parse(event.data));
  //   store.response = JSON.parse(event.data);
  // };
  // store.connection.onopen = function () {
  //   console.log("Successfully connected to the echo websocket server...");
  //   backendConnected.value = true;
  // };
  // store.connection.onclose = function () {
  //   // backendConnected.value = false;
  // };
  // store.connection.onerror = function () {
  //   // backendConnected.value = false;
  // };
});
</script>

<template>
  <div v-if="backendConnected">
    <NavBar class="fixed top-0 left-0 right-0 z-50" />
    <router-view class="absolute top-16 left-0 right-0" v-slot="{ Component }">
      <AnimationHandler
        :animation="
          routerState.routes.get(`${store.prevRoute} ${store.currRoute}`)
        "
      >
        <component :is="Component" />
      </AnimationHandler>
    </router-view>
  </div>
  <div v-else>
    <ErrorPage />
  </div>
</template>
