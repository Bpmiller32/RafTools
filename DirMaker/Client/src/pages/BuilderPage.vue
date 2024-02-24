<script setup lang="ts">
import { useGlobalState } from "../store";
import ParascriptBuilderCard from "../components/ParascriptBuilderCard";

const state = useGlobalState();
</script>

<template>
  <div v-if="state.beConnection.value.data == null">loading....</div>
  <div v-else class="ml-6 mt-6 flex gap-10 flex-wrap">
    <ParascriptBuilderCard
      :crawlermodule="
        JSON.parse(state.beConnection.value.data).Parascript.Crawler
      "
      :buildermodule="
        JSON.parse(state.beConnection.value.data).Parascript.Builder
      "
    />
  </div>
</template>
