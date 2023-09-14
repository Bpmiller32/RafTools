<script setup lang="ts">
import { useGlobalState } from "../store";
import CrawlerCard from "../components/CrawlerCard";

const state = useGlobalState();
</script>

<template>
  <div v-if="state.beConnection.value.data == null">loading....</div>
  <div v-else class="p-6 flex space-x-10 flex-wrap">
    <CrawlerCard
      name="SmartMatch"
      :status="JSON.parse(state.beConnection.value.data).SmartMatch.Crawler"
    ></CrawlerCard>
    <!-- <CrawlerCard name="Parascript"></CrawlerCard>
    <CrawlerCard name="RoyalMail"></CrawlerCard> -->
  </div>
</template>
