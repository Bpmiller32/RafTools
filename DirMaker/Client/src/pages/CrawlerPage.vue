<script setup lang="ts">
import { useGlobalState } from "../store";
import CrawlerCard from "../components/CrawlerCard";
import CrawlerList from "../components/CrawlerList";

const state = useGlobalState();
</script>

<template>
  <div v-if="state.beConnection.value.data == null">loading....</div>
  <div v-else class="p-6 flex space-x-10 flex-wrap">
    <div>
      <CrawlerCard
        id="TestIdForCrawlerCard"
        name="SmartMatch"
        :module="JSON.parse(state.beConnection.value.data).SmartMatch.Crawler"
      />
      <CrawlerList
        class="mt-6"
        name="SmartMatch"
        :directorylist="
          JSON.parse(state.beConnection.value.data).SmartMatch.Crawler
            .ReadyToBuild
        "
      />
    </div>
    <!-- <CrawlerCard name="Parascript"></CrawlerCard>
    <CrawlerCard name="RoyalMail"></CrawlerCard> -->
  </div>
</template>
