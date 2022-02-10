<template>
  <div class="flex-grow mx-5 truncate bg-white rounded-lg shadow">
        <div class="flex items-center justify-between p-6 flex-wrap sm:flex-nowrap">
            <div class="mr-14">
                <p class="text-gray-900 text-sm font-medium">Service Status</p>
                <p class="mt-2 text-gray-500 text-sm">Service Active: ✅</p>
                <p class="mt-2 text-gray-500 text-sm">Last Check-in: 09/09/2022</p>
                <p class="text-gray-500 text-sm">@ 10:11:53 am</p>
            </div>
        
            <LogFeed class="flex-grow max-w-md max-h-36 overflow-auto mt-2 sm:mt-0"/>
        </div>
  </div>
</template>

<script>
import LogFeed from "./LogFeed.vue"

export default {
  components: { LogFeed }

}
</script>
