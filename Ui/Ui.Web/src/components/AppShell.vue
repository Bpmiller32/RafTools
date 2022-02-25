<template>
    <Disclosure as="nav" class="bg-white shadow-sm" v-slot="{ open }">
        <div class="flex justify-between h-16 px-4 sm:px-6 lg:px-8 mx-auto">
            <div class="flex">
                <div class="flex items-center">
                    <img class="block lg:hidden h-8" src="https://tailwindui.com/img/logos/workflow-mark-indigo-600.svg"/>
                    <img class="hidden lg:block h-8" src="https://tailwindui.com/img/logos/workflow-logo-indigo-600-mark-gray-800-text.svg"/>
                </div>
                <div class="hidden sm:-my-px sm:ml-6 sm:flex sm:space-x-8">
                    <a v-for="item in navigation" :key="item.name" :href="item.href" :class="LinkStyleDesktop(item.current)">{{ item.name }}</a>
                </div>
            </div>
            <div class="flex items-center">
                <DisclosureButton class="sm:hidden bg-white p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                    <MenuIcon v-if="!open" class="h-6 w-6"/>
                    <XIcon v-else class="h-6 w-6"/>
                </DisclosureButton>
            </div>
        </div>
        <DisclosurePanel class="sm:hidden">
            <div class="pt-2 pb-3 space-y-1">
                <DisclosureButton v-for="item in navigation" :key="item.name" as="a" :href="item.href" :class="LinkStyleMobile(item.current)">{{ item.name }}</DisclosureButton>
            </div>
        </DisclosurePanel>
    </Disclosure>

    <div class="my-10">
        <header>
            <div class="mx-auto px-4 sm:px-6 lg:px-8">
                <h1 class="text-3xl font-bold leading-tight text-gray-900">
                    Directory Builder 📚
                </h1>
            </div>
        </header>
        <main>
            <div class="flex justify-center mt-10">
                <StatusCard :builderStatus="builderStatus"/>
            </div>

            <div v-if="builderStatus.active" class="flex flex-wrap justify-center mt-10">
                <BuildCard class="mx-5 mb-5" :dirType="'SmartMatch'" :builderStatus="builderStatus"/>   
                <BuildCard class="mx-5 mb-5" :dirType="'Parascript'" :builderStatus="builderStatus"/>   
                <BuildCard class="mx-5 mb-5" :dirType="'RoyalMail'" :builderStatus="builderStatus"/>       
            </div>
        </main>
    </div>
</template>

<script>
import { Disclosure, DisclosureButton, DisclosurePanel } from '@headlessui/vue'
import { MenuIcon, XIcon } from '@heroicons/vue/outline'
import BuildCard from './BuildCard.vue'
import StatusCard from './StatusCard.vue'
import axios from 'axios'

export default {
    name: "MainPage",
    components: {
        Disclosure,
        DisclosureButton,
        DisclosurePanel,
        MenuIcon,
        XIcon,
        BuildCard,
        StatusCard,
    },
    methods: {
        GetBuilderStatus() {
            axios.get(`https://localhost:5001/api/Builder`)
            .then(response => {
                if (response.data === null) {
                    console.log("Connected to server but no data")
                    return
                }
                
                // Set the service status
                this.builderStatus.active = true

                // Set the timestamp data
                const timestamp = new Date()
                this.builderStatus.timestamp.checkinDate =  timestamp.toLocaleDateString('en-us').toString()
                this.builderStatus.timestamp.checkinTime =  timestamp.toLocaleTimeString('en-us').toString()

                // Set the builder data
                this.builderStatus.builders.smartmatch.status = response.data.SmartMatch.Status
                this.builderStatus.builders.smartmatch.progress = response.data.SmartMatch.Progress
                this.builderStatus.builders.smartmatch.availableBuilds = response.data.SmartMatch.AvailableBuilds
                
                this.builderStatus.builders.parascript.status = response.data.Parascript.Status
                this.builderStatus.builders.parascript.progress = response.data.Parascript.Progress
                this.builderStatus.builders.parascript.availableBuilds = response.data.Parascript.AvailableBuilds

                this.builderStatus.builders.royalmail.status = response.data.RoyalMail.Status
                this.builderStatus.builders.royalmail.progress = response.data.RoyalMail.Progress
                this.builderStatus.builders.royalmail.availableBuilds = response.data.RoyalMail.AvailableBuilds
            })
            .catch(() => {
                this.builderStatus.active = false
            })
        },


        // Styling methods, should move these to inline in template later
        LinkStyleDesktop(currentItem) {
            if (currentItem) {
                return 'flex items-center text-gray-900 text-sm font-medium px-1 pt-1 border-b-2 border-blue-500'
            }
            else {
                return 'flex items-center text-gray-500 hover:text-gray-700 text-sm font-medium px-1 pt-1 border-b-2 border-transparent hover:border-gray-300'
            }
        },
        LinkStyleMobile(currentItem) {
            if (currentItem) {
                return 'bg-indigo-50 border-indigo-500 text-indigo-700 block pl-3 pr-4 py-2 border-l-4 text-base font-medium'
            }
            else {
                return 'border-transparent text-gray-600 hover:bg-gray-50 hover:border-gray-300 hover:text-gray-800 block pl-3 pr-4 py-2 border-l-4 text-base font-medium'
            }
        }
    },
    data() {
        return {
            navigation: [
                { name: 'Crawler', href: '#', current: false },
                { name: 'Builder', href: '#', current: true },
            ],

            builderStatus: {
                active: false,
                timestamp: {
                    checkinTime: "",
                    checkinDate: "",
                },
                builders: {
                    smartmatch: {
                        status: 0,
                        progress: 0,
                        availableBuilds: [],
                    },
                    parascript: {
                        status: 0,
                        progress: 0,
                        availableBuilds: [],
                    },
                    royalmail: {
                        status: 0,
                        progress: 0,
                        availableBuilds: [],
                    },
                }
            }
        }
    },
    created() {
        setInterval(() => 
            this.GetBuilderStatus(), 3000
        )
    },
}
</script>