<script setup>
import BuilderCard from "./BuilderCard.vue";
import { useStore } from "../store";

const store = useStore();

function BuildTest() {
  store.SendMessageTest("RoyalMail", "Force", "202206");
}
function BuildTest2() {
  store.SendMessageTest("Parascript", "Force", "202206");
}
</script>

<template>
  <div class="flex flex-wrap justify-evenly items-start">
    <div class="m-6">
      <button
        type="button"
        @click="BuildTest()"
        :class="{
          'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700': true,
          'bg-gray-500 bg-[length:0%,0%] cursor-not-allowed': false,
          'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
        }"
      >
        Test RoyalMail 202206
      </button>
      <button
        type="button"
        @click="BuildTest2()"
        :class="{
          'bg-indigo-600 bg-[length:150%,150%] hover:bg-[length:0%,0%] hover:bg-indigo-700': true,
          'bg-gray-500 bg-[length:0%,0%] cursor-not-allowed': false,
          'flex shrink-0 items-center my-4 pl-8 pr-2 py-2 max-h-8 bg-gradient-to-r from-indigo-600 to-indigo-600 bg-no-repeat bg-center border border-transparent text-sm leading-4 font-medium rounded-md shadow-sm text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500': true,
        }"
      >
        Test Parascript 202206
      </button>
    </div>
  </div>
</template>
