// 凌隐宝堂Jenkins Pipeline配置 - UltraThink重构CI/CD架构
// 支持企业内部Jenkins服务器

@Library('lybt-shared-library') _

pipeline {
    agent {
        label 'dotnet-agent'
    }
    
    environment {
        DOTNET_VERSION = '8.0'
        REGISTRY = 'registry.lybt.com'
        IMAGE_NAME = "${REGISTRY}/lybt/webapi"
        SONAR_HOST = 'https://sonarqube.lybt.com'
        NUGET_PACKAGES = "${WORKSPACE}/.nuget"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        GIT_COMMIT_SHORT = "${env.GIT_COMMIT.take(7)}"
    }
    
    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timestamps()
        timeout(time: 1, unit: 'HOURS')
        skipDefaultCheckout()
        disableConcurrentBuilds()
    }
    
    triggers {
        pollSCM('H/5 * * * *')
        cron('@daily')
    }
    
    parameters {
        choice(
            name: 'DEPLOY_ENV',
            choices: ['none', 'dev', 'staging', 'production'],
            description: '选择部署环境'
        )
        booleanParam(
            name: 'RUN_TESTS',
            defaultValue: true,
            description: '是否运行测试'
        )
        booleanParam(
            name: 'SECURITY_SCAN',
            defaultValue: true,
            description: '是否执行安全扫描'
        )
    }
    
    stages {
        stage('Checkout') {
            steps {
                cleanWs()
                checkout scm
                script {
                    env.GIT_BRANCH = sh(returnStdout: true, script: 'git rev-parse --abbrev-ref HEAD').trim()
                    env.GIT_COMMIT_MSG = sh(returnStdout: true, script: 'git log -1 --pretty=%B').trim()
                    env.GIT_AUTHOR = sh(returnStdout: true, script: 'git log -1 --pretty=%ae').trim()
                }
            }
        }
        
        stage('Environment Setup') {
            steps {
                script {
                    echo "🔧 设置构建环境..."
                    echo "分支: ${env.GIT_BRANCH}"
                    echo "提交: ${env.GIT_COMMIT_SHORT}"
                    echo "作者: ${env.GIT_AUTHOR}"
                    echo "消息: ${env.GIT_COMMIT_MSG}"
                    
                    // 安装.NET SDK
                    sh '''
                        if ! command -v dotnet &> /dev/null; then
                            wget https://dot.net/v1/dotnet-install.sh
                            chmod +x dotnet-install.sh
                            ./dotnet-install.sh --version ${DOTNET_VERSION}
                        fi
                        dotnet --version
                    '''
                }
            }
        }
        
        stage('Restore Dependencies') {
            steps {
                echo "📦 恢复NuGet包..."
                sh 'dotnet restore LYBT.Backend.sln'
            }
        }
        
        stage('Code Quality') {
            parallel {
                stage('Code Format') {
                    steps {
                        echo "🔍 检查代码格式..."
                        sh '''
                            dotnet tool install -g dotnet-format || true
                            dotnet format LYBT.Backend.sln --verify-no-changes --verbosity diagnostic
                        '''
                    }
                }
                
                stage('Static Analysis') {
                    steps {
                        echo "📊 静态代码分析..."
                        withSonarQubeEnv('SonarQube') {
                            sh '''
                                dotnet tool install -g dotnet-sonarscanner || true
                                dotnet sonarscanner begin \
                                    /k:"lybt-backend" \
                                    /d:sonar.host.url="${SONAR_HOST}" \
                                    /d:sonar.login="${SONAR_TOKEN}"
                                dotnet build LYBT.Backend.sln
                                dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"
                            '''
                        }
                    }
                }
            }
        }
        
        stage('Build') {
            steps {
                echo "🔨 构建项目..."
                sh '''
                    dotnet build LYBT.Backend.sln \
                        --configuration Release \
                        --no-restore \
                        /p:Version=${BUILD_NUMBER} \
                        /p:FileVersion=${BUILD_NUMBER} \
                        /p:InformationalVersion=${GIT_COMMIT_SHORT}
                '''
            }
        }
        
        stage('Test') {
            when {
                expression { params.RUN_TESTS == true }
            }
            parallel {
                stage('Unit Tests') {
                    steps {
                        echo "🧪 运行单元测试..."
                        sh '''
                            dotnet test tests/LYBT.Tests.Unit/LYBT.Tests.Unit.csproj \
                                --configuration Release \
                                --no-build \
                                --logger "trx;LogFileName=unit-tests.trx" \
                                --collect:"XPlat Code Coverage" \
                                --results-directory ./TestResults
                        '''
                    }
                }
                
                stage('Integration Tests') {
                    steps {
                        echo "🧪 运行集成测试..."
                        sh '''
                            docker-compose -f docker-compose.test.yml up -d
                            sleep 10
                            
                            dotnet test tests/LYBT.Tests.Integration/LYBT.Tests.Integration.csproj \
                                --configuration Release \
                                --no-build \
                                --logger "trx;LogFileName=integration-tests.trx" \
                                --results-directory ./TestResults
                            
                            docker-compose -f docker-compose.test.yml down
                        '''
                    }
                }
            }
            post {
                always {
                    // 发布测试结果
                    mstest testResultsFile: '**/*.trx'
                    
                    // 发布代码覆盖率
                    publishCoverage adapters: [
                        coberturaAdapter('**/coverage.cobertura.xml')
                    ], 
                    sourceFileResolver: sourceFiles('STORE_LAST_BUILD')
                }
            }
        }
        
        stage('Security Scan') {
            when {
                expression { params.SECURITY_SCAN == true }
            }
            parallel {
                stage('Dependency Check') {
                    steps {
                        echo "🔒 依赖漏洞扫描..."
                        sh '''
                            dotnet list package --vulnerable --include-transitive
                            
                            # OWASP Dependency Check
                            dependency-check.sh \
                                --project "LYBT" \
                                --scan . \
                                --format "ALL" \
                                --enableExperimental
                        '''
                    }
                }
                
                stage('Security Scan') {
                    steps {
                        echo "🔍 安全代码扫描..."
                        sh '''
                            dotnet tool install -g security-scan || true
                            security-scan LYBT.Backend.sln
                        '''
                    }
                }
            }
        }
        
        stage('Publish') {
            steps {
                echo "📦 发布应用..."
                sh '''
                    dotnet publish src/Backend/Services/LYBT.WebAPI/LYBT.WebAPI.csproj \
                        --configuration Release \
                        --output ./publish \
                        --no-build \
                        --runtime linux-x64 \
                        --self-contained false
                '''
                
                // 归档制品
                archiveArtifacts artifacts: 'publish/**', fingerprint: true
            }
        }
        
        stage('Docker Build') {
            steps {
                echo "🐳 构建Docker镜像..."
                script {
                    docker.withRegistry("https://${REGISTRY}", 'docker-credentials') {
                        def customImage = docker.build(
                            "${IMAGE_NAME}:${GIT_COMMIT_SHORT}",
                            "-f src/Backend/Services/LYBT.WebAPI/Dockerfile ."
                        )
                        
                        customImage.push()
                        
                        if (env.GIT_BRANCH == 'main') {
                            customImage.push('latest')
                        }
                        
                        if (env.GIT_BRANCH == 'develop') {
                            customImage.push('dev')
                        }
                    }
                }
            }
        }
        
        stage('Container Scan') {
            steps {
                echo "🔍 扫描容器镜像..."
                sh """
                    trivy image \
                        --severity HIGH,CRITICAL \
                        --format json \
                        --output trivy-report.json \
                        ${IMAGE_NAME}:${GIT_COMMIT_SHORT}
                """
                
                recordIssues(
                    enabledForFailure: true,
                    tool: trivy(pattern: 'trivy-report.json')
                )
            }
        }
        
        stage('Deploy') {
            when {
                expression { params.DEPLOY_ENV != 'none' }
            }
            steps {
                script {
                    def deployEnv = params.DEPLOY_ENV
                    def namespace = "lybt-${deployEnv}"
                    def kubeconfig = "${deployEnv.toUpperCase()}_KUBECONFIG"
                    
                    echo "🚀 部署到 ${deployEnv} 环境..."
                    
                    withCredentials([file(credentialsId: kubeconfig, variable: 'KUBECONFIG')]) {
                        if (deployEnv == 'production') {
                            // 生产环境需要审批
                            input message: '确认部署到生产环境?', 
                                  ok: '部署',
                                  submitter: 'admin,lead-developer'
                            
                            // 蓝绿部署
                            sh """
                                kubectl set image deployment/lybt-webapi-green \
                                    webapi=${IMAGE_NAME}:${GIT_COMMIT_SHORT} \
                                    -n ${namespace}
                                
                                kubectl rollout status deployment/lybt-webapi-green -n ${namespace}
                                
                                # 切换流量
                                kubectl patch service lybt-webapi -n ${namespace} \
                                    -p '{"spec":{"selector":{"version":"green"}}}'
                                
                                sleep 60
                                
                                # 更新蓝色环境
                                kubectl set image deployment/lybt-webapi-blue \
                                    webapi=${IMAGE_NAME}:${GIT_COMMIT_SHORT} \
                                    -n ${namespace}
                            """
                        } else {
                            // 非生产环境直接部署
                            sh """
                                kubectl set image deployment/lybt-webapi \
                                    webapi=${IMAGE_NAME}:${GIT_COMMIT_SHORT} \
                                    -n ${namespace}
                                
                                kubectl rollout status deployment/lybt-webapi -n ${namespace}
                            """
                        }
                    }
                }
            }
        }
        
        stage('Smoke Test') {
            when {
                expression { params.DEPLOY_ENV != 'none' }
            }
            steps {
                echo "🧪 运行冒烟测试..."
                script {
                    def envUrl = getEnvironmentUrl(params.DEPLOY_ENV)
                    
                    sh """
                        # 等待服务就绪
                        sleep 30
                        
                        # 健康检查
                        curl -f ${envUrl}/health || exit 1
                        
                        # 运行API测试
                        newman run tests/postman/smoke-tests.json \
                            --env-var "baseUrl=${envUrl}" \
                            --reporters cli,json \
                            --reporter-json-export smoke-test-results.json
                    """
                    
                    // 发布测试结果
                    publishHTML([
                        allowMissing: false,
                        alwaysLinkToLastBuild: true,
                        keepAll: true,
                        reportDir: '.',
                        reportFiles: 'smoke-test-results.json',
                        reportName: 'Smoke Test Results'
                    ])
                }
            }
        }
    }
    
    post {
        always {
            echo "🧹 清理工作空间..."
            cleanWs(cleanWhenNotBuilt: false,
                    deleteDirs: true,
                    disableDeferredWipeout: true,
                    notFailBuild: true)
        }
        
        success {
            echo "✅ 构建成功!"
            notifyBuild('SUCCESS')
        }
        
        failure {
            echo "❌ 构建失败!"
            notifyBuild('FAILURE')
        }
        
        unstable {
            echo "⚠️ 构建不稳定!"
            notifyBuild('UNSTABLE')
        }
    }
}

def getEnvironmentUrl(env) {
    switch(env) {
        case 'dev':
            return 'https://dev.lybt.com'
        case 'staging':
            return 'https://staging.lybt.com'
        case 'production':
            return 'https://lybt.com'
        default:
            return 'http://localhost'
    }
}

def notifyBuild(String buildStatus) {
    def color = 'RED'
    def emoji = '❌'
    
    if (buildStatus == 'SUCCESS') {
        color = 'GREEN'
        emoji = '✅'
    } else if (buildStatus == 'UNSTABLE') {
        color = 'YELLOW'
        emoji = '⚠️'
    }
    
    def message = """
        ${emoji} *Jenkins构建通知*
        
        项目: ${env.JOB_NAME}
        构建: #${env.BUILD_NUMBER}
        状态: ${buildStatus}
        分支: ${env.GIT_BRANCH}
        提交: ${env.GIT_COMMIT_SHORT}
        作者: ${env.GIT_AUTHOR}
        
        [查看详情](${env.BUILD_URL})
    """
    
    // 发送通知到Slack
    slackSend(
        color: color,
        message: message,
        channel: '#ci-cd'
    )
    
    // 发送邮件
    emailext(
        subject: "${emoji} Jenkins Build: ${env.JOB_NAME} - ${buildStatus}",
        body: message,
        to: 'dev-team@lybt.com',
        recipientProviders: [developers(), requestor()]
    )
}