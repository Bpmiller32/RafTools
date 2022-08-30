<script setup>
import BuilderCard from "./BuilderCard.vue";
</script>

<template>
  <div class="flex flex-wrap justify-evenly items-start">
    <div class="m-6">
      <BuilderCard dirType="Parascript"></BuilderCard>
    </div>
    <div class="m-6">
      <BuilderCard dirType="RoyalMail"></BuilderCard>
    </div>
  </div>
</template>
