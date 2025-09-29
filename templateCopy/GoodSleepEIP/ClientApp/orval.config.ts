import { defineConfig } from 'orval';

export default defineConfig({
    petstore: {
        output: {
            mode: 'split',
            target: './src/service/apiServices.ts', // 生成的 API 和型別文件位置
            client: 'axios',
            override: {
                mutator: {
                    path: './src/service/apiClient.ts', // 自定義 axios 實例
                    name: 'customInstance'
                },
                operations: {
                    '*': {
                        transformer: {
                            case: 'none' // 保留原始字段名稱，none |pascalCase | camelCase
                        }
                    }
                }
            }
        },
        input: {
            target: 'http://localhost:5566/swagger/v1/swagger.json' // 後端 Swagger JSON 文件的 URL
        }
    }
});
