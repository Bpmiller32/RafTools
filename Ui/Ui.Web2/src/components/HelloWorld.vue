<script setup>
import { ref } from "vue";
import { useStore } from "../store";

const connection = useStore();

function setImage() {
  return "";
}

function testSend() {
  connection.connection.send("hello");
}

defineProps({
  msg: String,
  connection: WebSocket,
});

const count = ref(0);
</script>

<template>
  <h1>{{ msg }}</h1>

  <p>
    Recommended IDE setup:
    <a href="https://code.visualstudio.com/" target="_blank">VS Code</a>
    +
    <a href="https://github.com/johnsoncodehk/volar" target="_blank">Volar</a>
  </p>

  <img :src="setImage()" alt="" />

  <p>
    <a href="https://vitejs.dev/guide/features.html" target="_blank">
      Vite Documentation
    </a>
    |
    <a href="https://v3.vuejs.org/" target="_blank">Vue 3 Documentation</a>
  </p>

  <button type="button" @click="connection.count++">
    count is: {{ connection.count }}
  </button>
  <button type="button" @click="testSend()">
    Send a WebSocket test message
  </button>

  <div v-if="connection.response">
    {{ connection.response }}
  </div>

  <p>
    Edit
    <code>components/HelloWorld.vue</code> to test hot module replacement.
  </p>
</template>
