#!groovy

tag="${env.RELEASE_TAG}"

pipeline {
    agent { label 'ubuntu-ci' }
    environment {
        REPO_NAME="github.com/ShiftLeftSecurity/SharpSyntaxRewriter"
        JFROG_USER="jenkins-ci"
        GITHUB_KEY = '4b3482c3-735f-4c31-8d1b-d8d3bd889348'
    }
    options{
        skipDefaultCheckout()
    }
    stages {
        stage('setPolling') {
            steps {
                script {
                    sshagent (credentials: ["${env.GITHUB_KEY}"]) {
                        git poll: false, url: "ssh://git@${env.REPO_NAME}"
                    }
                }
            }
        }
        stage('cleanUp') {
            steps {
                script {
                    try {
                        deleteDir()
                    } catch (err) {
                        println("WARNING: Failed to delete directory: " + err)
                    }
                }
            }
        }
        stage('getSrc') {
            steps {
                script {
                    echo "Building on master branch with tag ${env.RELEASE_TAG}"
                    sshagent (credentials: ["${env.GITHUB_KEY}"]) {
                        checkout([$class: 'GitSCM',
                                  branches: [[name: "refs/tags/${env.RELEASE_TAG}"]],
                                  doGenerateSubmoduleConfigurations: false,
                                  extensions: [[$class: 'SubmoduleOption',
                                                disableSubmodules: false,
                                                parentCredentials: false,
                                                recursiveSubmodules: true,
                                                reference: '',
                                                trackingSubmodules: false],
                                               [$class: 'RelativeTargetDirectory',
                                                relativeTargetDir: 'SharpSyntaxRewriter']],
                                  submoduleCfg: [],
                                  userRemoteConfigs: [[credentialsId: '4b3482c3-735f-4c31-8d1b-d8d3bd889348',
                                                       url: "ssh://git@${env.REPO_NAME}"]]])
                    }
                }
            }
        }
        stage('buildAndPackAndPush') {
            steps {
                script {
                    FAILED_STAGE=env.STAGE_NAME
                    dir ("${WORKSPACE}/SharpSyntaxRewriter") {
                        sh "dotnet build -c Release src/SharpSyntaxRewriter/SharpSyntaxRewriter.csproj"
                        sh "dotnet pack -c Release src/SharpSyntaxRewriter/SharpSyntaxRewriter.csproj -o ."
                        sh "nuget push 'SharpSyntaxRewriter.{RELEASE_TAG}.nupkg' -Source Artifactory"
                    }
                }
            }
        }
    }
}
