<script setup>
import { useStore } from "../store";
import BuilderCard from "./BuilderCard.vue";

const store = useStore();
</script>

<template>
  <div
    v-if="
      // store.builders.SmartMatch.DataRecieved &&
      store.builders.Parascript.DataRecieved &&
      store.builders.RoyalMail.DataRecieved
    "
    class="flex flex-wrap justify-evenly items-start"
  >
    <div class="m-6">
      <BuilderCard dirType="Parascript"></BuilderCard>
    </div>
    <div class="m-6">
      <BuilderCard dirType="RoyalMail"></BuilderCard>
    </div>
  </div>
</template>
