<!-- This example requires Tailwind CSS v2.0+ -->
<template>
    <div class="flex-grow max-w-md truncate bg-white rounded-lg shadow divide-y divide-gray-200">
        <div class="flex items-center justify-between p-6">
            <div>
                <div class="flex">
                    <p class="text-gray-900 text-sm font-medium">{{ dirType }}</p>
                    <p class="ml-3 px-2 py-0.5 text-green-800 text-xs font-medium bg-green-100 rounded-full">Ready</p>
                </div>
                
                <p class="mt-2 text-gray-500 text-sm">Select directory month:</p>
                
                <SelectMenu/>
            </div>
        
            <img class="w-20 h-20 bg-gray-300 rounded-full" :src="avatar"/>
        </div>
        
        <div class="flex justify-between">           
            <div class="flex flex-grow mx-3 my-8 p-0.5 bg-gray-200 rounded-full">
                <p class="bg-indigo-500 p-0.5 text-xs font-medium text-white text-center leading-none rounded-full" :style="{width: `${progress}%`}">{{ progress }}%</p>
            </div>
        
            <button @click="TestSubmit()" class="flex items-center px-3 mr-6 my-4 rounded-md bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                <MailIcon class="mr-2 h-4 w-4 text-white"></MailIcon>
                <p class="text-sm leading-4 font-medium text-white">Build</p>
            </button>
        </div>
    </div>
</template>

<script>
import { MailIcon, PhoneIcon } from '@heroicons/vue/solid'
import SelectMenu from './SelectMenu.vue'
import axios from 'axios'

export default {
    name: "BuildCard",
    props: {
        dirType: String,
        progress: Number,
    },
    components: {
        MailIcon,
        PhoneIcon,
        SelectMenu,
    },
    methods: {
        TestSubmit() {
            axios.get(`http://jsonplaceholder.typicode.com/posts`)
            .then(response => {
                // JSON responses are automatically parsed.
                console.log(response.data)
            })
            .catch(e => {
                this.errors.push(e)
            })
        }
    },
    data() {
        return {
            avatar: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=4&w=256&h=256&q=60',
        }
    }
}
</script>