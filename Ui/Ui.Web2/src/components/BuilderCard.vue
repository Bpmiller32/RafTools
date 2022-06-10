<script setup>
import { useStore } from "../store";

const store = useStore();

function ButtonClickedPara() {
  store.SendMessageTest("Parascript", "Force", "202206");
}
function ButtonClickedRoyal() {
  store.SendMessageTest("RoyalMail", "Force", "202205");
}
</script>
<template>
  <button
    type="button"
    @click="ButtonClickedPara()"
    :class="{
      'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700': true,
      'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
    }"
  >
    Parascript
  </button>
  <button
    type="button"
    @click="ButtonClickedRoyal()"
    :class="{
      'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700': true,
      'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
    }"
  >
    RoyalMail
  </button>
</template>
