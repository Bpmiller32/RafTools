<script setup>
// This starter template is using Vue 3 <script setup> SFCs
// Check out https://vuejs.org/api/sfc-script-setup.html#script-setup
import { onMounted, ref } from "vue";
import { useStore } from "./store";
import NavBar from "./components/NavBar.vue";
import ErrorPage from "./components/ErrorPage.vue";

const store = useStore();
const backendConnected = ref(true);

onMounted(() => {
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
    <NavBar />
    <router-view />
  </div>
  <div v-else>
    <ErrorPage />
  </div>
</template>
