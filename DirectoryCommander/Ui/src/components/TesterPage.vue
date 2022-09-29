<script setup>
import TesterCard from "./TesterCard.vue";
import { useStore } from "../store";
import LoadingPage from "./LoadingPage.vue";

const store = useStore();
</script>

<template>
  <div
    v-if="
      store.testers.SmartMatch.DataRecieved &&
      store.testers.Parascript.DataRecieved &&
      store.testers.RoyalMail.DataRecieved &&
      store.testers.Zip4.DataRecieved
    "
    class="flex flex-wrap justify-evenly items-start"
  >
    <div class="m-6">
      <div class="m-6"><TesterCard dirType="SmartMatch" /></div>
    </div>
    <div class="m-6">
      <div class="m-6"><TesterCard dirType="Zip4" /></div>
    </div>
    <div class="m-6">
      <div class="m-6"><TesterCard dirType="Parascript" /></div>
    </div>
    <div class="m-6">
      <div class="m-6"><TesterCard dirType="RoyalMail" /></div>
    </div>
  </div>
  <LoadingPage v-else />
</template>
