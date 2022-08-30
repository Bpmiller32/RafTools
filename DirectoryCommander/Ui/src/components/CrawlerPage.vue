<script setup>
import CrawlerCard from "./CrawlerCard.vue";
import CrawlerList from "./CrawlerList.vue";
</script>

<template>
  <div class="flex flex-wrap justify-evenly items-start">
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
</template>
