<script setup>
import { useStore } from "../store";
import CrawlerCard from "./CrawlerCard.vue";
import CrawlerList from "./CrawlerList.vue";
import LoadingPage from "./LoadingPage.vue";

const store = useStore();
</script>

<template>
  <div
    v-if="
      store.crawlers.SmartMatch.DataRecieved &&
      store.crawlers.Parascript.DataRecieved &&
      store.crawlers.RoyalMail.DataRecieved
    "
    class="flex flex-wrap justify-evenly items-start"
  >
    <div class="m-6">
      <CrawlerCard dirType="SmartMatch" />
      <CrawlerList dirType="SmartMatch" class="mt-6" />
    </div>
    <div class="m-6">
      <CrawlerCard dirType="Parascript" />
      <CrawlerList dirType="Parascript" class="mt-6" />
    </div>
    <div class="m-6">
      <CrawlerCard dirType="RoyalMail" />
      <CrawlerList dirType="RoyalMail" class="mt-6" />
    </div>
  </div>
  <LoadingPage v-else />
</template>
